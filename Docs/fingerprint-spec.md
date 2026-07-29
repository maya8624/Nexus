# Fingerprint — Log Triage System Spec (MVP)

Handoff spec for implementation across three codebases: React frontend, Python
(rec_brain) service, .NET backend. Originally the pre-implementation spec; this
version is **updated to match the as-built system** (backend phases 1–6 complete).

Companion docs in this repo:

- `Docs/fingerprint-implementation-plan.md` — the .NET build plan and design
  decisions (authoritative for backend internals).
- `Docs/fingerprint-api-contract.md` — the REST API contract (authoritative for
  the React dashboard; §8 below is a summary only).

**Implementation status:** .NET backend (ingest, fingerprinting, classification,
routing, GitHub actor, REST API) is built and unit-tested. Python `/classify` and
`/summarize` are live on rec_brain. Remaining: config wiring + manual Azure/GitHub
setup (plan Phase 7), the React dashboard, and the post-MVP items in §10.

---

## 1. What the system does (one paragraph)

Fingerprint pulls warning+ telemetry from **Azure Application Insights**
(workspace-based, via the Azure Monitor Query API / KQL), deduplicates events into
**fingerprints** (App Insights `problemId` for exceptions; message-template hash
for non-exception events), classifies each fingerprint into an actionable
category, routes it to a GitHub assignee via a deterministic ownership map, and
files/updates GitHub issues idempotently. Eligible issues can be handed to a
coding agent that opens a PR — the agent never touches main; CI must pass and a
human always merges.

**Core principle (applies everywhere):** rules first, LLM fallback only for
ambiguous cases. All side effects (DB writes, GitHub calls) live in .NET. Python
is stateless.

**Ingest is an adapter boundary.** MVP ships one source adapter: App Insights
(Query API). A file-based adapter (Serilog CLEF/JSON rolled files, with
self-built fingerprinting) is the planned v2 extension — everything downstream of
the fingerprint table is source-agnostic and must stay that way. (The
`ingest_cursor` table is already keyed by `source` for this reason.)

---

## 2. Architecture and ownership

```
Azure App Insights (Log Analytics workspace)
      │
      ▼
[.NET] FingerprintIngestJob (Hangfire, every 15 min) ── Azure.Monitor.Query + KQL
      │   exceptions grouped by problemId; traces severityLevel == 2
      ▼
[.NET] FingerprinterService ── normalize to fingerprint id, upsert counts in Postgres
      │
      ▼
[.NET] FingerprintRuleClassifierService ── known patterns → category; unknown → call Python
      │                                                        │
      │                                       [Python] LLM classify + summarize
      │                                                        │
      ▼                                                        ▼
[.NET] FingerprintRouter ── ownership map (appsettings JSON) → GitHub assignee
      │
      ▼
[.NET] GitHubIssueService (Octokit) ── create / update / reopen / close issue, labels
      │
      ▼
[React] Dashboard ── reads .NET API, renders fingerprint table + detail
```

### Responsibility split

| Layer | Owns |
|---|---|
| .NET (Nexus) | Hangfire ingest job, Azure Monitor Query client (KQL), fingerprint normalization, Postgres state, rule classifier, ownership map, all GitHub API calls (Octokit), REST API for dashboard |
| Python (rec_brain, FastAPI) | Two stateless endpoints: `POST /classify` (categorize an unknown fingerprint) and `POST /summarize` (write GitHub issue title/body/suggested-fix from a fingerprint cluster). Called by .NET via the existing `X-API-Key` header. No DB access. |
| React | Dashboard SPA. Reads .NET REST API only. No direct Python or GitHub access. |

---

## 3. Data model (Postgres) — as built

EF Core code-first (`Nexus.Infrastructure`), snake_case naming. Enums are stored
as **PascalCase strings** (`HasConversion<string>()`) — a deliberate deviation
from the repo's int-enum norm, because `category`/`github_status` double as
GitHub label text and this is a human triage surface.

```sql
-- fingerprints (actual columns)
id                          TEXT PRIMARY KEY,    -- 'fp_' + first 8 hex of hash, max 16 chars
hash                        TEXT NOT NULL UNIQUE,-- SHA-256 of source key (see §4)
level                       TEXT NOT NULL,       -- 'Error' | 'Warning'
category                    TEXT,                -- PascalCase taxonomy value, NULL until classified
classified_by               TEXT,                -- 'Rule' | 'Llm'
exception_type              TEXT,                -- NULL for trace-origin fingerprints
message_template            TEXT NOT NULL,       -- normalized message / outerMessage
operation                   TEXT,                -- nullable (operation_Name not always populated)
service_name                TEXT,                -- nullable (cloud_RoleName not always populated)
total_count                 INT NOT NULL,
first_seen_utc              TIMESTAMPTZ NOT NULL,
last_seen_utc               TIMESTAMPTZ NOT NULL,
auto_fix_eligible           BOOLEAN NOT NULL,
github_issue_number         INT,
github_status               TEXT NOT NULL,       -- 'None' | 'Open' | 'Closed' | 'Pr' | 'Merged'
github_issue_filed_at_utc   TIMESTAMPTZ,         -- set on issue creation; powers "issues assigned today"
github_last_commented_at_utc TIMESTAMPTZ,        -- powers the 1-hour comment throttle
created_at_utc              TIMESTAMPTZ NOT NULL,
updated_at_utc              TIMESTAMPTZ NOT NULL
-- indexes: unique(hash), (github_status, level)

-- fingerprint_occurrences
id               BIGSERIAL PRIMARY KEY,
fingerprint_id   TEXT NOT NULL REFERENCES fingerprints(id) ON DELETE CASCADE,
occurred_at      TIMESTAMPTZ NOT NULL,   -- window timestamp (per-poll aggregate, not per-event)
occurrence_count INT NOT NULL,           -- count within this poll window
rendered_message TEXT,                   -- one sample message from the window
created_at_utc   TIMESTAMPTZ NOT NULL
-- index: (fingerprint_id, occurred_at) for sparkline queries

-- ingest_cursor (one row per source adapter)
source          TEXT PRIMARY KEY,        -- 'appinsights'
last_polled_to  TIMESTAMPTZ NOT NULL,    -- exclusive upper bound of last committed window
updated_at_utc  TIMESTAMPTZ NOT NULL
```

Differences from the original draft spec, all deliberate:

- **No `source`/`source_key` columns on `fingerprints`** — the source is encoded
  in the hash prefix (`"appinsights|exception|…"`), and the v2 file adapter will
  use its own prefix; a queryable column can be added if ever needed.
- **No `sample_trace` column** — the representative sample lives on
  `fingerprint_occurrences.rendered_message` (up to 10 recent shown in the API).
- **No `assignee` column** — the router computes the assignee at issue-creation
  time from config; persisting it would go stale when the ownership map changes.
- **`github_status` values** are `None/Open/Closed/Pr/Merged` (not
  `new/issue/pr/merged/resolved`): `Closed` covers both "resolved by human" and
  "closed on GitHub"; a closed fingerprint that recurs is reopened with a
  "regressed" comment.
- **Two extra timestamps** (`github_issue_filed_at_utc`,
  `github_last_commented_at_utc`) exist to make the stats endpoint accurate and
  the comment throttle implementable.

Sparkline data = occurrences bucketed per hour, last 7 buckets.

---

## 4. Ingest + fingerprinting (App Insights adapter, .NET)

Hangfire recurring job (`fingerprint-ingest`, `*/15 * * * *`). Query window =
`[last_polled_to, now - 5 min)` — the 5-min lag lets App Insights ingestion
latency settle. First-ever poll (no cursor row) looks back 60 min. The cursor
advances **only after** the whole poll batch commits.

Client: `Azure.Monitor.Query` (`LogsQueryClient`) with `DefaultAzureCredential`.
Time bounds are passed via the SDK's `QueryTimeRange` parameter, **not**
string-interpolated into the KQL text.

**Query 1 — exceptions (level = Error):** grouped by
`problemId, type, operation_Name, cloud_RoleName` with count, sample message, and
first/last timestamps. Fingerprint key = `problemId` (App Insights' own exception
grouping). `hash = SHA256("appinsights|exception|" + problemId)`.

**Query 2 — warnings (traces, severityLevel == 2):** grouped by
`message, operation_Name, cloud_RoleName`. `traces.message` is the **rendered**
message, so it's normalized before hashing to approximate a template — strip
quoted strings → `{str}`, GUIDs → `{guid}`, digit runs ≥ 3 → `{n}`, collapse
whitespace (case untouched). `hash = SHA256("appinsights|trace|" + normalizedMessage)`.
(Emitting the template as a custom dimension from Serilog's App Insights sink is
the proper fix; noted as a follow-up.)

**Per grouped row:** read-then-branch upsert on `fingerprints` (absence of the
row **is** the new-fingerprint signal, passed explicitly to the classifier);
increment `total_count`, update `last_seen_utc`; insert one
`fingerprint_occurrences` row for the window.

Idempotency: deterministic id re-derived from the hash (no DB round-trip), the
cursor guarantees non-overlapping windows, and a failed poll retries the same
window safely.

*(v2 file adapter — post-MVP: parse Serilog CLEF rolled files; only the adapter
changes, everything downstream stays identical.)*

---

## 5. Classification taxonomy

Categories (classify what a human should DO, not the exception type). C# enum
values are PascalCase; the Python contract, GitHub labels, and config lists use
SCREAMING_SNAKE_CASE (`FingerprintCategoryWireFormat` converts):

| Category (wire) | Rule heuristics (checked first, in .NET, fixed priority) |
|---|---|
| `NEW_REGRESSION` | Fingerprint never seen before — short-circuits before any other rule or LLM call |
| `DEPENDENCY_FAILURE` | Timeout/connection exceptions targeting an external client (HttpClient, DB connect) |
| `CONFIG_AUTH` | 401/403 to dependencies, missing config keys, secret/connection-string exceptions, pool exhaustion |
| `DATA_QUALITY` | Validation/parse failures, FormatException, JSON deserialization errors |
| `PERFORMANCE` | Timeout-flavored warnings without hard failure |
| `RECURRING_KNOWN` | Existing fingerprint with count spike (> 3× hourly baseline; baseline averages the whole occurrence history — accepted MVP tradeoff) |

If no rule matches **and** the fingerprint is still unclassified → .NET calls
Python `POST /classify` with `{exception_type, message_template, sample_trace, operation}`
→ returns `{category, confidence, reason}` (field renamed from the draft's
`rationale`; both sides now use `reason`). Stored as `classified_by = 'Llm'`.
Classification is sticky — a later rule can override a stale LLM result, but a
failed/unmatched LLM call just leaves the fingerprint unclassified and it's
retried next poll. An AI failure never aborts the poll batch.

**Auto-fix eligibility** — computed by the classifier, gated on
`AutoFixAllowlistCategories` / `AutoFixDenylistNamespaces` in
`FingerprintRoutingSettings` (denylist always wins), matched against the row's
`ProblemId`. Trace-origin rows have no `ProblemId`, so they **fail closed** —
never auto-fix eligible.

---

## 6. Routing (ownership map)

**JSON in `appsettings.json`** (bound via `IOptions<FingerprintRoutingSettings>`,
same as every other settings class — not a separate YAML file). Array order =
evaluation order, first match wins:

```json
"FingerprintRoutingSettings": {
  "Ownership": [
    { "Match": { "ServiceName": "nexus-api", "OperationPrefix": "Nexus.Payments" }, "Assignee": "andy-payments" },
    { "Match": { "ServiceName": "rag-service" }, "Assignee": "andy-ai" },
    { "Match": { "Category": "DEPENDENCY_FAILURE" }, "Assignee": "andy-ops" }
  ],
  "DefaultAssignee": "andy",
  "AutoFixAllowlistCategories": [ "DATA_QUALITY", "NEW_REGRESSION", "CONFIG_AUTH" ],
  "AutoFixDenylistNamespaces": [ "Nexus.Payments", "Nexus.Auth" ]
}
```

No LLM involvement in routing. Ever.

---

## 7. GitHub integration (.NET, Octokit) — idempotency rules

Keyed off `github_status`, per fingerprint, after each poll:

- `None` + threshold met → create issue (title/body from Python `POST /summarize`),
  assign via router, label `severity/<level>` + `category/<CATEGORY>`, store issue
  number + `github_issue_filed_at_utc`, status → `Open`. When the fingerprint is
  auto-fix eligible and Python returned a `suggested_fix`, a `## Suggested Fix`
  section is appended to the body.
- `Open` → do NOT create; add a count comment, throttled to max one per hour via
  `github_last_commented_at_utc`.
- `Closed` and the fingerprint reappears → reopen with a "regressed" comment.
- `Pr` / `Merged` / below threshold → no-op.
- Noise threshold: count ≥ 3 within the poll window, **unless** it's a new
  regression (file immediately).
- All GitHub/AI failures inside the poll pipeline are swallowed and retried next
  poll (batch safety). The DB is saved immediately after each GitHub side effect
  so a crash can't lose an issue number and file a duplicate.
- Manual triggers (REST): force-file (skips threshold), add `auto-fix-candidate`
  label, and close ("resolve"). These do **not** swallow errors and return
  `Conflict` results for invalid states.
- Agent loop (post-MVP hook): the `auto-fix-candidate` label is the entire hook;
  GitHub Actions workflow → coding agent → PR. One CI-failure retry max, then
  label `needs-human` and keep the ownership-map assignee.

Python `POST /summarize` input: fingerprint + last 5 occurrences. Output:
`{title, body, suggested_fix}` (`suggested_fix` nullable) — body includes
plain-language summary, first/last seen, count, affected operation, suggested
first debugging step.

---

## 8. .NET REST API (consumed by React)

Built. **See `Docs/fingerprint-api-contract.md` for the authoritative contract**
(shapes, enum values, error codes, button-enable logic). Summary:

```
GET  /api/fingerprints?status=Open&level=Error    → list + 7-bucket sparkline
GET  /api/fingerprints/{id}                       → detail incl. recent occurrences
POST /api/fingerprints/{id}/file-issue            → manual trigger, skips threshold
POST /api/fingerprints/{id}/send-to-agent         → adds auto-fix-candidate label
POST /api/fingerprints/{id}/resolve               → closes the GitHub issue, status → Closed
GET  /api/stats                                   → { openErrors, openWarnings,
                                                      issuesAssignedToday, agentPrsAwaitingReview }
```

Notes vs. the original draft: query/enum values are PascalCase strings
(`Open`, `Error`); **resolve requires an existing open GitHub issue** (409
`NoGithubIssue` otherwise — there is no issue-less "mark resolved" in MVP); JWT
Bearer auth is required on all endpoints (standard app auth, single-user in
practice, multi-tenant out of scope).

---

## 9. Dashboard UI spec (React)

Name: **Fingerprint**. A working HTML mockup exists
(`fingerprint-dashboard.html`) — replicate its layout and behavior in React;
visual style may follow the existing frontend design system instead of the
mockup's palette.

Layout:

- **Header**: brand ("Fingerprint · log triage · <service>"), last-poll time,
  source label (App Insights workspace name).
- **Stat strip** (4 cards): open error fingerprints, open warning fingerprints,
  issues assigned today, agent PRs awaiting review (always 0 until the agent
  loop exists). Data from `GET /api/stats`.
- **Main grid**: fingerprint table (left, ~2/3) + detail panel (right, ~1/3).
  Stacks vertically on mobile.

Fingerprint table columns:

1. Level badge (`Error` red-tinted, `Warning` amber-tinted, dot + label)
2. Signature: fingerprint id (mono, muted) + message template + operation
   (small, muted, second line)
3. Category (taxonomy string; show an "unclassified" muted state for `null`)
4. Frequency sparkline (7 hourly buckets, tiny bar chart; buckets arrive newest
   first and sparse — reverse and pad client-side)
5. Assignee (avatar initials + name — derive from ownership map display config;
   the API does not return an assignee field)
6. Status pill from `githubStatus`: `None` → "not yet filed" / `Open` →
   "issue #N" / `Pr` → "PR open · #N" / `Merged` → "merged · #N" / `Closed` →
   "resolved · #N"

Row click → detail panel:

- Fingerprint id, message template, operation
- Key/value rows: level, category, total occurrences, first seen, last seen,
  auto-fix eligible
- Recent occurrences list (`renderedMessage`, mono, scrollable, max-height
  ~120px) — this is the "sample trace" surface
- Actions (enable rules mirror the server): primary "File GitHub issue"
  (`githubStatus` is `None` or `Closed`), "View on GitHub #N" (whenever
  `githubIssueNumber` exists), "Send to agent for fix" (`autoFixEligible` and
  issue exists), "Mark resolved" (`githubStatus === "Open"`)

States: loading skeleton for table; empty state ("No open fingerprints — logs
are clean"); error state with retry.

Sorting default: last seen desc (the API already returns this order). Filter:
level (all/error/warning) and status.

---

## 10. MVP scope boundaries

**In (built)**: App Insights ingest (Query API + KQL, polled), fingerprinting via
problemId/normalized message, rule classification with LLM fallback, ownership
routing, idempotent GitHub issue creation/update/reopen/close, REST API for the
dashboard read + 3 manual actions.

**Remaining for MVP**: real config values + manual Azure/GitHub setup
(`fingerprint-implementation-plan.md` Phase 7), the React dashboard. Dev-seed
mock data exists: `dotnet run --project tools/FingerprintSeed` (see
`fingerprint-api-contract.md` → "Mock data for development").

**Out (post-MVP)**: file-based ingest adapter (Serilog CLEF rolled files,
self-built stack-frame fingerprinting — planned v2 extension), agent auto-fix
workflow (design specified, hook is the label), GitHub webhooks (until then
`Pr`/`Merged` are never set automatically), daily digest, multi-tenant,
notification channels (Slack/email).

---

## 11. Conventions followed (actual project patterns)

- .NET: layered services returning `Result<T>` + repositories/Unit of Work (this
  repo does **not** use MediatR); Hangfire for background jobs; Python calls via
  the existing `AiServiceSettings` base URL + `X-API-Key` header pattern.
- Python: FastAPI, Pydantic v2 models, stateless; part of the existing rec_brain
  sidecar.
- Postgres: same instance as the rest of Nexus; EF Core migrations
  (`dotnet ef migrations add ... --project Nexus.Infrastructure --startup-project Nexus.Api`).
- Config (ownership map, allow/denylists, GitHub settings) = `appsettings.json`
  sections bound via `IOptions<T>`, blank placeholders in the base file, real
  values in `appsettings.Development.json` / Key Vault — not separate config
  files, not DB.
- Azure Monitor access via `DefaultAzureCredential` (`az login` locally, managed
  identity deployed); grant the identity **Log Analytics Reader** on the
  workspace. GitHub PAT with `repo` scope in Key Vault.
