# Fingerprint — Domain Applications (exploration)

**Status: exploratory. Nothing here is committed scope.** This doc takes the
fingerprint pipeline built in `Docs/fingerprint-implementation-plan.md` and asks
what else it can be pointed at — specifically in real estate and accounting, the
two domains this codebase already touches.

Companion docs:

- `Docs/fingerprint-implementation-plan.md` — the as-built .NET design (authoritative
  for internals; everything referenced below by class name lives there).
- `Docs/fingerprint-spec.md` — cross-codebase spec.
- `Docs/fingerprint-api-contract.md` — REST contract.

The spec already states the intent this doc builds on:

> **Ingest is an adapter boundary.** MVP ships one source adapter: App Insights.
> Everything downstream of the fingerprint table is source-agnostic and must stay
> that way.

That sentence is the whole premise. What follows is eight concrete candidates for
what else could sit upstream of that boundary.

---

## 1. The reusable core

Strip the error-triage vocabulary out of the built system and what remains is:

```
high-volume noisy stream
      │
      ▼
normalize + hash into a stable entity        ← the collapse
      │
      ▼
count occurrences per window vs. baseline    ← the signal
      │
      ▼
classify (rules first, LLM fallback)         ← the label
      │
      ▼
decide if it deserves human attention        ← the filter
      │
      ▼
act idempotently, keep acting as state moves ← the state machine
```

Nothing in that pipeline is about software errors. `Fingerprint` is "a recurring
problem with an identity," `FingerprintOccurrence` is "a sighting," and
`GitHubIssueService` is "an actor that maintains one external record per problem."

**One-line framing:** the system turns an unbounded event stream into a bounded,
stateful worklist.

### Where the value actually sits

Worth being precise, because it determines which domains are worth the effort:

| Step | Contribution |
|---|---|
| Dedupe (`FingerprintHasher`, `MergeByHash`) | **Most of it.** 10,000 events → 12 entities. Without collapse you have a log viewer with an LLM bolted on. |
| Baseline / spike (`GetHourlyBaselineAsync`, `MinSpikeMultiplier`) | Separates "this happened" from "something changed." |
| Classification | Useful, but a commodity. Any LLM does this. |
| Idempotent actor (`GitHubIssueService`) | Where most of the code went. Calling an API once is trivial; not calling it nine times is the work. |

Classification is the part that demos well and the part that matters least.

---

## 2. Fit test — apply before designing anything

### 2.1 Does the source produce *repeating* items?

If 10,000 inputs collapse to ~12 entities, the system is transformative. If 10,000
inputs stay 10,000 entities, you have built a classifier with a ticket API — still
useful, but the dedupe, baseline, merge, and idempotency machinery is dead weight.

| Repeats well | Doesn't repeat |
|---|---|
| DB constraint violations, failed job rows | Individual contracts |
| Validation errors, feed rejections | Resumes, offer letters |
| Claim denials, decline codes | Invoices *as documents* |
| Sensor alarms, maintenance symptoms | Property valuations |
| Support tickets, e-file rejects | One-off legal filings |

### 2.2 If items are unique — fingerprint the *finding*, not the artifact

This is the move that rescues most "unique document" domains.

Five hundred contracts are all different. But the **clause deviations found across
them** repeat — "payment terms exceed 60 days" shows up forty times. Same with
invoices: the document is unique, but "field `total` failed to extract on Acme's
template" recurs every week.

Drop the unit of analysis one level and the collapse comes back:

```
artifact-level  →  500 contracts, 500 fingerprints, no value
finding-level   →  500 contracts → 1,800 findings → 23 fingerprints
```

Every idea in §4 and §5 that involves documents uses this shift.

---

## 3. What changes when the source isn't a live event stream

The existing seams are in the right places. These are genuinely swappable:

| Seam | Currently | Swap to |
|---|---|---|
| Source adapter | `AppInsightsQueryService` (KQL) | DB query, file scan, API poll |
| Hash key | `severity\|problemId` | domain-specific tuple |
| Classifier | `FingerprintRuleClassifierService` + `/classify` | domain rule table, same shape |
| Actor | `GitHubIssueService` (Octokit) | Jira, ServiceNow, CMMS, email, in-app queue |
| Filing policy | `FingerprintFilingPolicy` | domain thresholds |
| Router | `FingerprintRouter` | domain ownership map |

But two structural things break, and they are the same two for every non-stream
source in this doc.

### 3.1 Baseline and spike detection weaken

`GetHourlyBaselineAsync` and `MinSpikeMultiplier` assume a time dimension with a
meaningful "normal rate." A one-shot DB scan or nightly file drop has no such
thing. Replacements, roughly in order of preference:

- **Scan-over-scan delta** — `NEW_REGRESSION` becomes "not present in the previous
  scan." Cheap and usually enough.
- **Absolute thresholds per category** — crude but predictable, and finance/ops
  users tend to prefer it because it's explainable.
- **Peer-group comparison** — e.g. this building vs. other buildings, this office
  vs. other offices. Stronger signal than time-based baselines for the real estate
  cases, since it isolates *systematic* behaviour.

### 3.2 Idempotency shifts from cursor to content hash

This is the sharp edge. The current design is safe because a committed time window
is never re-polled — the cursor advances inside the batch commit deliberately, and
the plan is explicit that re-polling is **not** idempotent for counters:

> `UpdateFingerprint` re-runs `TotalCount += count` and `AddOccurrenceAsync` inserts
> a duplicate row, since `(FingerprintId, OccurredAt)` is a plain `HasIndex`, not
> `IsUnique`.

Re-scanning a *file* or a *table*, unlike re-polling a time window, is a completely
normal operation. Someone will re-run the nightly scan. So any batch/snapshot source
needs one of:

- a processed-source-item marker (`source_item_hash` per occurrence), or
- making `(FingerprintId, SourceItemHash)` genuinely `IsUnique` so the occurrence
  insert itself is the guard, or
- replacing `TotalCount += count` with a recomputed absolute count per scan.

The third is cleanest for snapshot sources ("how many open exceptions exist right
now" is a better question than "how many have ever existed"), but it changes the
meaning of `TotalCount` and would need the sparkline logic revisited.

**Do not port a snapshot source without resolving this first.** It is the one place
where the existing design is genuinely coupled to a streaming source.

---

## 4. Real estate

### 4.1 Listing feed drift

**Problem.** Listing syndication is chronically broken. Feeds arrive from MLSs,
portals, and partner CRMs, each with its own dialect. When a provider silently
changes a field, thousands of listings go bad and nobody notices until an agent
complains days later.

**Hash key.** `(feed_source, validation_rule, field)`

**Collapse.** ~8,000 rejected listings → ~15 distinct problems.

```
Nightly / hourly feed ingest
      │
      ▼
Validation pass ── emits one finding per (listing, rule, field)
      │
      ▼
MergeByHash ── 8,000 findings → 15 fingerprints
      │
      ▼
Baseline: scan-over-scan delta
      │  new fingerprint = provider changed something last night
      ▼
Classifier (rules): SchemaDrift | UnitMismatch | RequiredFieldMissing
                    | EnumUnknown | GeocodeFailure | MediaBroken
      │
      ▼
Router: feed_source → data-ops owner
      │
      ▼
Actor: internal ticket + Slack to the feed owner
```

**Suggested-fix playbooks by category.** These are deterministic, not LLM output:

| Category | Playbook |
|---|---|
| `UnitMismatch` | Apply conversion, backfill affected listings |
| `EnumUnknown` | Add mapping to the feed's enum table |
| `SchemaDrift` | Contact provider; pin to previous schema version |
| `GeocodeFailure` | Re-run geocode with fallback provider |

**AutoFixEligible maps cleanly here.** Safe normalizations (unit conversion, phone
formatting, enum remaps, whitespace) go on the allowlist. Anything touching price,
square footage, or address goes on the denylist — the existing
`AutoFixDenylistNamespaces` pattern works unchanged, just keyed on field name
rather than namespace. And the existing fail-closed behaviour when the key is
absent is exactly right.

**Fit: strong.** High repeat rate, obvious owner, immediate value, and this repo
already has `PropertyEnums.cs` and an ingestion pipeline.

---

### 4.2 Maintenance request dedupe (property management)

**Problem.** Thirty tenants in one building report "no hot water" over two hours.
That becomes thirty work orders, thirty dispatches, and no one notices it's one
boiler.

This is the industrial alarm-flood problem wearing a different hat.

**Hash key.** `(building_id, system, symptom_normalized)`

**Collapse.** 30 requests → 1 work order. More importantly, the *spread across
units* is itself the diagnostic signal.

```
Tenant requests (app, phone, email)
      │
      ▼
NormalizeSymptom ── free text → symptom code
      │              (this is where the LLM earns its keep)
      ▼
MergeByHash on (building, system, symptom)
      │
      ▼
Peer-group baseline: this building vs. portfolio
      │  ├─ 1 unit affected  → unit-level fault, normal work order
      │  └─ N units affected → building-level fault, escalate
      ▼
Classifier: HVAC | Plumbing | Electrical | Appliance | Structural | Pest
      │
      ▼
Router: (building, trade) → assigned vendor
      │
      ▼
Actor: CMMS work order — one per fingerprint, not per request
       + auto-reply to every tenant on the fingerprint
```

**Note the actor's second job.** Unlike GitHub issues, there's a *many-to-one*
notification obligation back to the reporters. The occurrence rows already model
this — each occurrence carries the reporting tenant, so "notify everyone attached
to this fingerprint" is a natural query. That's a genuine extension to the actor
interface, not a rename.

**The comment throttle matters more here than in error triage.** `GithubLastCommentedAtUtc`
becomes "don't re-notify this vendor every hour about the same fault."

**Fit: strong.** Clear buyer (property managers feel this monthly), and the dedupe
value is legible to a non-technical audience in one sentence.

---

### 4.3 Compliance and disclosure gap detection

**Problem.** Brokerages get fined for missing disclosures. Per-transaction review
catches individual misses; it never surfaces that one office skips the same form
every time.

**Unit of analysis: the finding, not the transaction file** (§2.2).

**Hash key.** `(jurisdiction, disclosure_type, missing_field)`

**Collapse.** 1,200 transaction files → ~4,000 findings → ~30 fingerprints.

```
Transaction file set (periodic scan — snapshot source, see §3.2)
      │
      ▼
Rule pass per jurisdiction ── emits findings
      │
      ▼
MergeByHash
      │
      ▼
Peer-group baseline: office vs. office, agent vs. agent
      │  concentration is the signal — a fingerprint spread evenly
      │  across offices is a training gap; one concentrated in a
      │  single office is a process failure
      ▼
Classifier: MissingDisclosure | ExpiredForm | UnsignedDocument
            | JurisdictionMismatch | DeadlineMissed
      │
      ▼
Router: jurisdiction → regional compliance officer
      │
      ▼
Actor: compliance ticket + audit trail entry
```

**Design note.** This is a snapshot source, so §3.2 applies in full. The natural
model is recompute-per-scan rather than `TotalCount += count`, because the useful
question is "how many open gaps exist right now," not "how many have ever existed."

**Fit: good.** Obvious buyer with a budget (fines are a line item). Caveat:
jurisdiction rule tables are a real ongoing maintenance cost, and that cost is the
actual product moat — worth going in eyes open.

---

### 4.4 Tenant application fraud patterns

**Problem.** Fake paystubs and forged bank statements in rental applications.

**Why it's a fit at all.** Fraud is usually a poor fingerprinting candidate because
each case looks unique. But rings **reuse templates** — the same doctored paystub
layout, the same fabricated employer, the same phone number. So the fingerprints
genuinely repeat, which is unusual for fraud work and is precisely what makes this
viable.

**Hash key.** `(document_type, inconsistency_type, artifact_signature)`

where `artifact_signature` is the template-level tell — font metrics, layout hash,
employer name, originating phone — not the applicant.

```
Application documents
      │
      ▼
Extraction + consistency checks
      │
      ▼
MergeByHash on the template signature, NOT the applicant
      │
      ▼
Cross-property baseline
      │  same signature at 3+ properties = ring, not coincidence
      ▼
Classifier: TemplateForgery | ArithmeticInconsistency
            | EmployerUnverifiable | MetadataTampering
      │
      ▼
Actor: flag for human review — NEVER auto-reject
```

**Hard constraint: `AutoFixEligible` must be permanently false here.** Automated
adverse action against a rental applicant is a fair-housing and FCRA problem, not
an engineering tradeoff. The actor's only move is "route to a human with the
evidence attached." Worth writing into the config rather than relying on
convention — an allowlist that is empty by construction.

**Fit: moderate.** Genuinely valuable and the repeat structure is real, but the
compliance surface is significant and it needs legal review before it goes near
production. Lowest priority of the four.

---

### 4.5 Weak fit, for contrast — valuation and comp anomalies

**Hash key.** There isn't a good one. Every property is unique; every valuation is
a continuous number, not a discrete recurring event.

You would get one fingerprint per property, which is §2.1's failure mode exactly.
Anomalous valuations are a statistics problem — z-scores against comparable sets —
not a fingerprinting problem. **Don't build this on this pipeline.**

Included here because it's the most tempting bad fit in the domain, and the reason
it fails is the fastest way to internalize the fit test.

---

## 5. Accounting

### 5.1 AP invoice exception triage — the strongest candidate

**Problem.** Invoices land in an exception queue — no PO match, tax mismatch,
missing approval, unreadable field. AP clerks work the queue item by item, forever,
never seeing that 400 of the 2,000 exceptions are one vendor's template change.

**This repo is already most of the way there:** `InvoiceExtractionJob`,
`AiInvoiceExtractionResponse`, the `Invoice` upload purpose, and the whole
fingerprint pipeline all exist. The new code is a hash key, a rule table, and an
actor swap.

**Unit of analysis: the exception, not the invoice** (§2.2). Each invoice is
unique; the exceptions recur.

**Hash key.** `(vendor_id, exception_type, field)`

**Collapse.** ~2,000 queued exceptions → ~20 root causes.

```
InvoiceExtractionJob (exists)
      │
      ▼
Extraction result + 3-way match pass
      │  emits one exception per (invoice, type, field)
      ▼
MergeByHash ── 2,000 exceptions → 20 fingerprints
      │
      ▼
Baseline: rolling 30-day per vendor
      │  new fingerprint on a known vendor = template drift
      ▼
Classifier (rules cover most of this):
      PoMismatch | PoMissing | QuantityVariance | PriceVariance
      | TaxMismatch | DuplicateSubmission | ApprovalMissing
      | TemplateDrift | ExtractionLowConfidence
      │
      ▼
Router: vendor → AP owner / buyer
      │
      ▼
Actor: AP workflow queue item, one per fingerprint
       + "apply to all 400" bulk resolution
```

**Better severity signal than the error-triage version has.** Extraction confidence
scores are richer than `SeverityLevel`, and exceptions carry a dollar amount — so
severity can be *value-weighted*. One $2M exception outranks 400 × $12. That's a
better `FingerprintFilingPolicy` input than raw count, and it's a genuine
improvement over what the error-triage version can do.

**Suggested-fix playbooks:**

| Category | Playbook |
|---|---|
| `PoMismatch` | Match on PO #, check 3-way tolerance, escalate to buyer |
| `TemplateDrift` | Re-map vendor template; re-run extraction on the cluster |
| `DuplicateSubmission` | Auto-void the later submission (allowlist candidate) |
| `TaxMismatch` | Verify jurisdiction rate; check vendor tax registration |
| `ApprovalMissing` | Route to approver by amount threshold |

**Bulk resolution is the killer feature and it falls out of the data model for
free.** Because occurrences hang off the fingerprint, "resolve all 400 invoices on
this fingerprint" is one query. That is the thing that makes an AP manager care,
and it doesn't exist in item-by-item queues.

**Fit: strongest in the doc.** Named cost centre with a budget, most existing code
reused, clearest demo.

---

### 5.2 Reconciliation break clustering

**Problem.** Month-end close stalls on reconciliation breaks. Controllers
re-triage the same recurring timing differences every single month.

**Hash key.** `(account, break_type, counterparty)`

**Why the existing category model fits unusually well.** Controllers *already*
think in exactly the `RECURRING_KNOWN` vs `NEW_REGRESSION` split — "known timing
difference, clears next month" versus "this is new, investigate now." The built
taxonomy needs almost no translation.

```
Ledger + bank/sub-ledger snapshot (period close)
      │
      ▼
Match pass ── unmatched items become breaks
      │
      ▼
MergeByHash
      │
      ▼
Baseline: prior periods for this account
      │  ├─ seen in 6 of last 6 closes → RecurringKnown, auto-annotate
      │  └─ never seen before          → NewBreak, escalate
      ▼
Classifier: TimingDifference | FxRevaluation | FeeNotBooked
            | DuplicatePosting | MissingAccrual | Unexplained
      │
      ▼
Router: account → responsible accountant
      │
      ▼
Actor: close-checklist item + carry-forward annotation
```

**Snapshot source — §3.2 applies.** Period-close data gets re-run repeatedly during
close. Recompute-per-scan, not incremental counters.

**The pitch is close acceleration**, which finance orgs actively buy. Auto-annotating
known recurring breaks with last period's explanation removes real hours from a
deadline-bound process.

**Fit: strong.** Slightly harder to demo than AP because it needs realistic
multi-period data.

---

### 5.3 E-file rejection codes

**Problem.** Tax filings get rejected by jurisdiction with a code. During season,
volume is high and a single rule change generates thousands of identical rejects.

**Hash key.** `(jurisdiction, reject_code, form_type)`

**Collapse.** Exceptionally clean — reject codes are a controlled vocabulary, so
normalization is nearly free. This is the highest-collapse-ratio candidate in the
doc.

```
E-file submission responses
      │
      ▼
MergeByHash on (jurisdiction, code, form)
      │
      ▼
Baseline: same period last season + rolling this season
      │  new fingerprint mid-season = jurisdiction changed a rule
      ▼
Classifier: SchemaValidation | IdentityMismatch | DuplicateFiling
            | CalculationRejection | JurisdictionRuleChange
      │
      ▼
Router: jurisdiction → tax ops lead
      │
      ▼
Actor: ops ticket + client-facing status update on the cluster
```

**Fit: good, but seasonal.** Extremely sharp value during filing season and near
zero outside it. That shape suits a feature inside a broader product; it does not
suit a standalone one.

---

### 5.4 Expense policy violations

**Problem.** Expense tools flag hundreds of violations. Someone reviews them one
at a time. Nobody ever asks whether the *policy* is the problem.

**Hash key.** `(policy_rule, department, expense_category)`

**Collapse.** ~400 flagged expenses → ~6 policy gaps.

**The interesting finding is usually about the policy, not the employees.** When
120 violations across 9 departments all hit one rule, the rule is ambiguous or
stale — not 120 people cheating. Item-by-item review structurally cannot surface
that; only the aggregate can. This reframes the output from "enforcement" to
"policy maintenance," which is a much easier internal sell.

```
Expense submissions + policy engine
      │
      ▼
MergeByHash on (rule, department, category)
      │
      ▼
Concentration analysis
      │  ├─ spread across many departments → policy is ambiguous
      │  └─ concentrated in one person     → behaviour, route to manager
      ▼
Classifier: PolicyAmbiguity | ThresholdBreach | MissingReceipt
            | OutOfPolicyVendor | DuplicateClaim
      │
      ▼
Router: PolicyAmbiguity → finance policy owner
        others          → department manager
      │
      ▼
Actor: policy review item OR manager review, per category
```

**Note the router branches on category, not just on a key lookup** — a small
extension to `FingerprintRouter`, which currently maps ownership by key alone.

**Fit: moderate.** Lower dollar value than AP or recs, but the cheapest to build
and the friendliest demo — "your policy is broken, here's proof" lands well.

---

## 6. Comparison

| Idea | Collapse | Existing code reused | Buyer clarity | Build cost | Verdict |
|---|---|---|---|---|---|
| AP invoice exceptions | 2,000 → 20 | **High** | High | Low | **Build first** |
| Listing feed drift | 8,000 → 15 | High | Medium | Low | Strong second |
| Reconciliation breaks | 500 → 25 | Medium | High | Medium | Strong |
| Maintenance dedupe | 30 → 1 | Medium | High | Medium | Strong |
| Compliance gaps | 4,000 → 30 | Medium | High | **High** (rule tables) | Good, costly |
| E-file rejects | 5,000 → 12 | Medium | Medium | Low | Seasonal only |
| Expense violations | 400 → 6 | Medium | Medium | **Low** | Cheap demo |
| Application fraud | varies | Low | Medium | High (legal) | Defer |
| Valuation anomalies | none | — | — | — | **Don't** |

---

## 7. What would need to change in the code

### 7.1 Ports unchanged

- `FingerprintHasher` — domain-agnostic already.
- `MergeByHash` — the merge-before-staging logic is source-independent, and is
  load-bearing for every idea here for the same three reasons the plan documents.
- `FingerprintIngestJob`'s three-phase commit ordering — stage everything, one
  `SaveChanges` covering fingerprints + occurrences + cursor, *then* the actor.
  The orphaned-issue failure mode it prevents exists in every domain above.
- `FingerprintRouter`, `FingerprintFilingPolicy` — config shape works as-is
  (except §5.4's category-branching router).
- The classifier's rules-first / LLM-fallback ordering, and its swallow-and-retry
  behaviour on AI failure.

### 7.2 Needs generalizing

**The actor is coupled at the domain layer.** `Fingerprint` carries
`GithubIssueNumber`, `GithubStatus`, `GithubIssueFiledAtUtc`, and
`GithubLastCommentedAtUtc`. For a non-GitHub actor these want to become something
like `ExternalTicketRef` (system + id) and `TicketStatus`.

Two knock-on consequences worth flagging before anyone starts:

- The `HasConversion<string>()` deviation was justified partly because "category
  values double as literal GitHub label text." If the actor isn't GitHub, that
  justification weakens — though the "human/DB triage surface" half still holds, so
  the decision probably survives on its own merits. Re-derive it, don't assume it.
- `FingerprintCategoryWireFormat` exists to bridge PascalCase C# to
  SCREAMING_SNAKE_CASE for the Python contract, GitHub labels, and
  `AutoFixAllowlistCategories`. Two of those three reasons are GitHub-shaped.

**An `IIssueActor` abstraction above `IGitHubIssueService`.** All four idempotency
branches (create / throttled-comment / reopen / no-op) are domain-independent and
belong in a base class. Every actor needs the same four; only the API call differs.

**Snapshot-source idempotency** — §3.2. The single genuine design gap.

**Value-weighted severity** (§5.1) — `FingerprintFilingPolicy` currently takes a
count. Money-denominated domains want an amount too.

---

## 8. Recommendation

**Build AP invoice exception triage (§5.1) first.**

- Reuses the most existing code — extraction, blob ingestion, and the full
  fingerprint pipeline are all in place.
- Clearest buyer: AP exception handling is a named cost centre.
- Bulk resolution falls out of the existing occurrence model for free and is the
  feature that makes someone care.
- It is a stream-ish source (invoices arrive continuously), so §3.2's snapshot
  problem can be deferred rather than solved up front.

**Then listing feed drift (§4.1)** — same pipeline, different adapter, and it
exercises `AutoFixEligible` in a domain where auto-fix is genuinely safe for a
meaningful subset.

Both are additive. Neither requires touching the error-triage path.

### Open questions before committing to either

1. Does the actor generalize now, or does the first domain port just add a second
   concrete service and defer `IIssueActor` until there are three?
2. Is `TotalCount` still the right primitive once one domain wants "open right now"
   rather than "seen ever"?
3. Does value-weighted severity belong in `FingerprintFilingPolicy`, or in a
   domain-specific policy implementation behind the same interface?
