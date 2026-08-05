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

That sentence is the whole premise. What follows is a set of concrete candidates for
what else could sit upstream of that boundary: real estate and accounting in depth
(§4–§5, the two domains this codebase already touches), then eight further
industries treated more briefly (§6).

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

## 6. Beyond real estate and accounting

Eight further industries where the loop transplants. Treated more briefly than
§4–§5 — the steps that stay identical are omitted, and what's listed per case is
what actually *differs*, plus the one insight that makes each non-obvious.

Read this section for the **assumption breaks**. Collectively these eight surface
three limits of the current design that §4–§5 never hit: two cases break the
15-minute poll interval, two invert the rules/LLM ratio, and two force genuine model
extensions rather than config changes. Those are summarized in §6.9.

### 6.1 Payments — decline triage

**Problem.** 10–15% of card authorizations decline. Most are legitimate (insufficient
funds) and unfixable. A small slice is systemic and very fixable — but it's buried
under the legitimate noise, so nobody sees it until revenue reporting lands days
later.

**Hash key.** `(gateway, decline_code_normalized, issuer_BIN_range, merchant_category)`

**Collapse.** ~50,000 declines/day → ~25 patterns.

```
Auth responses (multi-gateway)
  → normalize decline codes  ← each gateway has its OWN code vocabulary;
                               this is the real NormalizeMessage analogue
  → merge by hash
  → baseline: per-BIN, per-hour
  → spike = issuer outage or a BIN range suddenly failing
```

**Categories.** `IssuerOutage` | `FraudRuleFalsePositive` | `MerchantConfig` |
`NetworkTimeout` | `SoftDecline` | `HardDecline`

**Actor.** PagerDuty page for ops, plus a feedback loop into retry policy.

**What's distinctive.** The baseline *is* the product here, more than in any other
case. A decline code in isolation carries zero signal — "insufficient funds" is the
system working correctly. Only rate-versus-baseline converts it into an alert. Strip
out `MinSpikeMultiplier` and nothing remains.

The soft/hard decline split also maps onto `AutoFixEligible` cleanly, and the
fail-closed posture is *financially* enforced: soft declines can be auto-retried,
while retrying hard declines triggers card-network penalties (both Visa and
Mastercard fine excessive retry behaviour). "Unknown code → don't auto-retry" isn't
defensive coding here, it's avoiding a fine.

**Assumption break.** A 15-minute poll is too slow — an issuer outage bleeds money
every minute. This wants sub-minute polling, which changes the ingest job's shape.

**Fit: excellent.** Highest tempo, clearest dollar value.

---

### 6.2 Healthcare — claim denial management

**Problem.** 5–15% of claims are denied, and roughly two-thirds are never reworked
because per-claim rework cost (~$25–$118) exceeds the perceived value. Denials
arrive as 835/ERA remittance files carrying CARC/RARC codes.

**Hash key.** `(payer, CARC/RARC_code, procedure_code, provider)`

**Collapse.** ~3,000 denials/month → ~40 patterns.

```
835 remittance files (batch drops — snapshot source, §3.2 applies)
  → parse to per-claim denial findings
  → merge by hash
  → baseline: per payer, per month
  → NEW fingerprint on a known payer = policy changed quietly
```

**Categories.** `PayerPolicyChange` | `CodingError` | `EligibilityLapse` |
`AuthorizationMissing` | `TimelyFilingMissed` | `DuplicateClaim` | `MedicalNecessity`

**Actor.** Worklist item in the billing system; router assigns by payer.

**What's distinctive.** `NEW_REGRESSION` is the entire value proposition. Payers
change policy without meaningful notice; catching it on day one instead of at
month-end close is the difference between reworking 40 claims and reworking 400.

**Model extension this case forces.** Claims have **appeal deadlines** — timely-filing
windows are typically 90–180 days. A fingerprint here therefore has a *shelf life*,
which the current model has no concept of. `FingerprintFilingPolicy` would need to
become deadline-aware: a low-count fingerprint nearing its filing deadline outranks
a high-count one with three months of runway. That's a real change, not a rename.

**Hard constraint.** PHI cannot go to a third-party LLM without a BAA. The
classifier's LLM fallback needs de-identified input or an in-boundary model. Resolve
this before design work, not after.

**Fit: strong**, and already a whole vendor category — which validates the loop but
means competition.

---

### 6.3 Security ops — alert dedupe

**Problem.** SOC analysts face thousands of alerts daily. Most are duplicates or
benign, so alert fatigue means real incidents get closed as noise.

**Hash key.** `(detection_rule, host_cohort, IOC_type)`

**Collapse.** ~10,000 alerts → ~30 incidents.

**Categories.** `TruePositiveConfirmed` | `BenignTruePositive` | `FalsePositiveTuning` |
`PolicyViolation` | `Reconnaissance` | `LateralMovement`

**Actor.** SOAR playbook or SIEM case; router by asset owner and on-call tier.

**What's distinctive.** `AutoFixEligible` becomes "which alerts may trigger
**automated containment**" — isolate a host, block an IP, disable an account. Same
allowlist/denylist shape, same fail-closed default, dramatically higher stakes:
auto-isolating a domain controller is a self-inflicted outage. The existing
"unknown key → not eligible" behaviour is exactly right and needs no change.

**A threat the error-triage version never faces.** Adversaries can *poison the
dedupe*. If grouping logic is predictable, an attacker floods the pipeline with one
pattern to bury a second, quieter one inside the same cluster. Design consequence:
dedupe must **group without suppressing** — occurrence counts and outlier members
stay individually inspectable, and clustering must never be the reason something
wasn't looked at. No other case here has an adversarial requirement.

**Fit: strong mechanically, crowded commercially.** Every SIEM and SOAR vendor sells
this; differentiation would be hard.

---

### 6.4 Manufacturing / IoT — alarm flood suppression

**Problem.** One root fault throws 500 alarms in minutes and operators go blind
precisely when they most need clarity. Severe enough to have its own standards —
ISA-18.2 and EEMUA 191 exist specifically to address it.

**Hash key.** `(asset_class, alarm_code, subsystem)`

**Collapse.** ~500 alarms → 3 actual faults.

**Categories.** `SensorFault` | `ProcessDeviation` | `EquipmentFailure` |
`UtilityLoss` | `SafetyInterlock` | `NuisanceAlarm`

**Actor.** CMMS work order; router by trade and shift. The comment throttle becomes
"don't re-page the same technician hourly."

**What's distinctive — and the reason this needs the most new machinery.** Hash
equality is not sufficient. Two *different* alarm codes firing seconds apart on
physically connected equipment are one event, and grouping them requires a **plant
topology model** plus a causal time window. That is genuinely beyond what
`MergeByHash` does today — correlation, not deduplication. This case needs the most
net-new code in the doc.

**One thing that comes free.** Baseline thresholds don't need inventing: ISA-18.2 and
EEMUA 191 publish target alarm rates per operator per hour, so the numbers are
industry-standardized rather than guessed.

**Non-negotiable.** Safety-critical alarms must never be suppressed by clustering,
regardless of count. Regulatory line, not a tuning parameter.

**Assumption break.** Latency budget is seconds, not 15 minutes.

**Fit: high value, highest build cost.**

---

### 6.5 Logistics — delivery exception clustering

**Problem.** Exceptions — failed delivery, damage, delay, bad address — scatter
across carriers and lanes. Ops teams react shipment by shipment and never see that
one hub degraded three weeks ago.

**Hash key.** `(carrier, lane, exception_reason_normalized)`

**Collapse.** ~4,000 exceptions → ~20 patterns.

```
Carrier EDI / API status events
  → normalize reason codes  ← every carrier has a different vocabulary
  → merge by hash
  → baseline: per lane, plus peer-group across comparable lanes
  → "hub X started failing this week" surfaces with nobody reading a spreadsheet
```

**Categories.** `HubCapacity` | `WeatherDisruption` | `AddressQuality` |
`CarrierPerformance` | `CustomsHold` | `PackagingFailure`

**What's distinctive.** The actor's output is *money recovery, not a fix*. A
fingerprint becomes evidence in a commercial negotiation — "your Memphis hub failed
340 shipments across six weeks" is an SLA claim or chargeback. Every other case here
produces a task for someone internal; this one produces a bill for someone external,
so the actor must emit an evidence package (the full occurrence list) rather than a
summary.

Geography also gives the cleanest peer-group baseline in the doc — comparable lanes
are a natural control group.

**Fit: good.** Clear ROI, but value scales with carrier-integration breadth, which is
unglamorous work.

---

### 6.6 Retail — returns and review mining

**Problem.** Return reasons and product reviews are free text. Nobody reads 10,000
reviews, and defective manufacturing batches surface weeks after they should.

**Hash key.** `(SKU, normalized_reason)` — plus `lot_code` where available, which
matters below.

**Collapse.** ~10,000 returns → ~30 issues.

**Categories.** `DefectManufacturing` | `SizingMismatch` | `DescriptionInaccurate` |
`ShippingDamage` | `BuyerRemorse` | `CounterfeitSuspected`

**Actor.** Quality ticket to merchandising or the supplier; router by category
manager.

**What's distinctive.** This is where the LLM stops being a *fallback* and becomes
the primary path. Free text has no `ProblemId` equivalent, so rules cover far less
ground — the ratio inverts from roughly 90% rules / 10% LLM to something like 30/70.

Two consequences follow. Cost becomes a real budget line at 10k classifications/day,
making the existing **classify-once-and-stick** rule far more valuable than it is
today. And classification quality becomes the product rather than a labelling
convenience, so it needs evaluation infrastructure the error-triage version never
needed.

**The high-value variant.** If lot or batch codes are available, include them in the
hash. "Spike on SKU" is interesting; "spike on lot #4471" is an actionable recall
scope. That single field turns a report into a decision.

**Fit: good.** Batch-defect early warning has genuine dollar value.

---

### 6.7 Customer support — ticket clustering

**Problem.** Support volume spikes and incidents get declared hours after customers
first noticed. Agents answer the same question 200 times without anyone noticing
it's one question.

**Hash key.** `(product_area, normalized_issue)`

**Collapse.** ~800 tickets → ~15 issues.

**Categories.** `OutageSuspected` | `BugConfirmed` | `UsabilityConfusion` |
`DocumentationGap` | `FeatureRequest` | `BillingDispute`

**Actor.** Jira issue plus a drafted status-page update; router by product area.

**What's distinctive — and this is the strategically interesting one.** It closes a
loop with the system already built. A support fingerprint spiking at the same time
as an error fingerprint is **one incident observed from both ends**: the error side
says what broke, the support side says who it hurt and how they describe it.

Correlating the two is something this repo is unusually well positioned to do,
because it already owns the error side outright. Nobody selling support-ticket
clustering has the exception stream; nobody selling error triage has the tickets.
That's a differentiator rather than a feature.

It also inherits §4.2's reverse notification obligation — everyone attached to the
cluster should hear about the resolution.

**Fit: strong**, and the correlation angle makes it the most interesting of the
eight.

---

### 6.8 Legal / compliance — audit finding dedupe

**Problem.** Audits generate hundreds of findings. The same control fails across a
dozen systems, but remediation is tracked per finding, so one systemic weakness
presents as forty unrelated problems.

**Hash key.** `(control_id, system, finding_type)`

**Collapse.** ~600 findings → ~25 systemic issues.

**Categories.** `AccessControl` | `ChangeManagement` | `DataRetention` |
`SegregationOfDuties` | `EvidenceGap` | `VendorRisk`

**Actor.** GRC platform item; router by control owner.

**What's distinctive — the category semantics invert.** In error triage,
`RECURRING_KNOWN` means "seen before, understood, deprioritize." In audit, a
**repeat finding across cycles is materially worse than a new one** — it evidences
failed remediation, escalates to management letters, and in regulated contexts may
require disclosure.

The same label carries opposite urgency. Anyone porting the taxonomy without
noticing would build something that systematically deprioritizes the findings that
matter most. It is the clearest example in this doc of why a classification
vocabulary can't be lifted between domains unexamined.

**Where the machinery barely applies.** Baseline is per audit cycle — quarterly or
annual. At that frequency there's no meaningful time-window logic; it's pure
scan-over-scan, and most of the ingest job's windowing does nothing.

**Fit: moderate.** Genuinely useful repeat-finding detection, but low event frequency
means the automation earns less than elsewhere.

---

### 6.9 Cross-case summary

| Case | Collapse | LLM reliance | Latency need | Biggest obstacle | Fit |
|---|---|---|---|---|---|
| Payments | 50k → 25 | Low | **Seconds** | Sub-minute polling redesign | **Excellent** |
| Support tickets | 800 → 15 | High | Minutes | Free-text normalization | **Strong** + correlation angle |
| Healthcare denials | 3k → 40 | Medium | Days | **PHI / BAA**, deadline model | Strong |
| Security ops | 10k → 30 | Low | Minutes | Crowded market, adversarial dedupe | Strong, hard to differentiate |
| Retail returns | 10k → 30 | **Very high** | Hours | LLM cost at volume | Good |
| Logistics | 4k → 20 | Medium | Hours | Carrier integration breadth | Good |
| Manufacturing | 500 → 3 | Low | **Seconds** | Needs topology + causal correlation | High value, highest cost |
| Audit findings | 600 → 25 | Medium | Weeks | Low frequency | Moderate |

**Three limits these eight expose**, none of which §4–§5 reach:

1. **The 15-minute poll is not universal.** Payments and manufacturing need seconds.
   The recurring-job cadence is currently a constant, not a per-source setting.
2. **Rules-first is not universal either.** Healthcare and retail invert the ratio,
   which changes both LLM cost and the need for classification evaluation.
3. **Two cases need model extensions, not config.** Healthcare's deadline-aware
   filing policy and manufacturing's topology-based causal correlation are both real
   additions to the domain model.

---

## 7. Comparison

Scoped to §4–§5 — the candidates that reuse this codebase directly. §6.9 has the
equivalent table for the eight cross-industry cases, scored on different axes
because code reuse isn't the deciding factor there.

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

## 8. What would need to change in the code

### 8.1 Ports unchanged

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

### 8.2 Needs generalizing

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

## 9. Recommendation

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
