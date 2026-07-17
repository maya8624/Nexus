# Fingerprint — Implementation Plan

Log-triage system: poll Azure App Insights, deduplicate warning/error telemetry into
"fingerprints", classify each one (rules first, LLM fallback), route it to a GitHub
assignee via an ownership map, and file/update GitHub issues idempotently.

This plan covers the **.NET side + the Python HTTP contract only**. React dashboard,
real Azure credential wiring, and the GitHub Actions coding-agent workflow are
explicitly out of scope (see [Out of scope](#out-of-scope)).

Everything below lives inside the existing `Nexus.sln` — no new solution/repo. The
Python `/classify` and `/summarize` endpoints are added to the existing external
`rec_brain` sidecar (not in this workspace); this doc defines their contract only.

---

## Architecture recap

```
Azure App Insights (Log Analytics workspace)
   │
   ▼
[.NET] FingerprintIngestJob (Hangfire, every 15 min) ── Azure.Monitor.Query + KQL
   │
   ▼
[.NET] FingerprinterService ── normalize to fingerprint id, upsert counts in Postgres
   │
   ▼
[.NET] FingerprintRuleClassifier ── known patterns → category; unknown → Python /classify
   │
   ▼
[.NET] FingerprintRouter ── ownership map (JSON config) → GitHub assignee (no LLM, ever)
   │
   ▼
[.NET] GitHubIssueService (Octokit) ── create / update / reopen issue, labels
   │
   ▼
[.NET] REST API ── consumed by a future React dashboard (not built here)
```

All side effects (DB writes, GitHub calls) live in .NET. Python is stateless — two
endpoints, no DB access, called via the same `X-API-Key` header pattern every other
`rec_brain` call already uses.

---

## Design decisions

These are the points where the spec's generic pseudocode doesn't match this repo's
actual conventions, and the call made for each, with reasoning.

### `fingerprints.id` — TEXT business key, not `Guid`

Keep `Id` as `string` (`fp_<hash prefix>`), not a `Guid` surrogate key.

- The id is deterministic — re-derived from `(source, problemId | normalizedMessage)`
  without a DB round-trip — which is exactly what makes the ingest upsert idempotent
  under retry. A surrogate `Guid` would need a `hash → id` lookup before every write
  anyway, so it buys nothing.
- It's also the GitHub-issue-idempotency key and the REST route key — `fp_a1b2c3` is
  far nicer in an issue title/URL or dashboard URL than a GUID.
- Not a foreign pattern here: `Order`/`Payment` already use `int` PKs, and every
  repository already defines its own typed `GetByIdAsync` rather than relying on the
  generic `IRepositoryBase<T>.Find(int id)`. `IFingerprintRepository.GetByIdAsync(string id, ct)`
  fits the same mold, just with a different key type.
- EF mapping mirrors `FileUploadConfiguration`: `builder.HasKey(x => x.Id); builder.Property(x => x.Id).HasMaxLength(16).ValueGeneratedNever();`
- `fingerprint_occurrences.id` stays `long`/`BIGSERIAL` (`ValueGeneratedOnAdd()`), a
  normal identity column. `ingest_cursor.source` is a small TEXT PK, same treatment
  as `fingerprints.id`.

### Dedicated `IFingerprintAiService`, not an extended `IAiService`

New, small `IFingerprintAiService` / `FingerprintAiService`, reusing
`AiServiceSettings.BaseUrl`/`ApiKey` (same physical `rec_brain` host — just two new
endpoint-path properties, `Classify` and `Summarize`), not a second settings class.

- `IAiService` is an 8-method interface tightly coupled to per-request concerns
  (`IUserContext`, `IUserRepository`, `IEnquiryRepository`). Fingerprint's calls come
  from a Hangfire job / GitHub-actor flow with **no HTTP user context at all**.
  Injecting `IAiService` there would drag in irrelevant dependencies.
- `FraudDetectionService` (`Nexus.Application/Services/FraudDetectionService.cs`) is
  direct repo precedent for exactly this shape: a small, separate service dedicated
  to one external Python-sidecar concern.
- Reusing `AiServiceSettings` avoids a second `BaseUrl`/`ApiKey` pointing at the same
  host — one fewer secret to rotate. If classify/summarize ever need a different
  host/key, splitting out a dedicated settings class later is a one-line change.
- Calling convention copies `AiService.BuildAiRequestOptions` exactly: `IHttpClientService`
  + `HttpRequestFactory.CreateHttpRequestMessage` + `X-API-Key` header + snake_case
  raw `Ai*Request`/`Ai*Response` DTOs mapped by hand to PascalCase `ReadModels` types.

### Ownership map — plain `IOptions<T>` binding a JSON array

```csharp
// Nexus.Application/Settings/FingerprintRoutingSettings.cs
public class FingerprintRoutingSettings
{
    public List<OwnershipRule> Ownership { get; init; } = new();
    public string DefaultAssignee { get; init; } = default!;
    public List<string> AutoFixAllowlistCategories { get; init; } = new();  // e.g. DataQuality, NewRegression, ConfigAuth
    public List<string> AutoFixDenylistNamespaces { get; init; } = new();   // e.g. "Nexus.Payments", "Nexus.Auth"
}

public class OwnershipRule
{
    public OwnershipMatch Match { get; init; } = new();
    public string Assignee { get; init; } = default!;
}

public class OwnershipMatch
{
    public string? ServiceName { get; init; }
    public string? OperationPrefix { get; init; }
    public string? Category { get; init; }
}
```

Bound via `services.Configure<FingerprintRoutingSettings>(config.GetSection(nameof(FingerprintRoutingSettings)))`
in `InfrasExtensions.cs`, same as every other settings class. `RateLimitSettings`
already proves nested/list-shaped config binds fine via plain `IOptions<T>` in this
repo — no `IOptionsMonitor<T>` (unused anywhere in the codebase today) or custom
JSON-file-loader needed. List order in the JSON array = evaluation order = "first
match wins". A redeploy to change ownership rules is acceptable for MVP; swapping to
`IOptionsMonitor<T>` later for no-redeploy edits is a one-line future change if ever
needed.

### One migration for all three new tables

Single migration `AddFingerprintTables` covering `fingerprints`,
`fingerprint_occurrences`, and `ingest_cursor` together — matches the repo's history
of "one migration per delivered unit" (e.g. `AddFileUploadsTable`, with
`AddPurposeToFileUploads` as a separate later follow-up once requirements actually
changed). No per-table migrations.

**Schema gap flagged**: the spec's `fingerprints` table has no field to support the
"max one comment per hour" idempotency rule. Adding `github_last_commented_at TIMESTAMPTZ NULL`
to `fingerprints` in this same migration — it's part of the entity's correct design,
not a later revision, and is required for the feature to actually implement the
stated idempotency rule.

### No new auth filter for GitHub calls

`GitHubSettings` (`Token`, `Owner`, `Repo`) is a plain `IOptions<T>` settings class,
same as `StripeSettings`/`PayPalSettings` — Key Vault in prod, blank placeholders in
`appsettings.json`. GitHub calls in this plan are 100% outbound (.NET → GitHub REST
API via Octokit + PAT `Credentials`). `InternalApiKeyFilter` only protects *inbound*
endpoints (`InternalController`) — there's no inbound GitHub webhook surface in this
MVP, so no filter is needed here. (If a future phase adds GitHub webhooks — e.g. to
auto-detect PR-merged and flip `github_status` to `merged` — that would need a new
inbound endpoint verified via GitHub's HMAC `X-Hub-Signature-256` scheme, not the
shared `X-Api-Key` scheme. Out of scope now, consistent with the spec calling the
agent loop a post-MVP hook.)

### App Insights adapter maps to plain `ReadModels`, not raw SDK types

`Azure.Monitor.Query`'s `LogsQueryResult`/`LogsTable`/`LogsTableRow` have no public
constructors suited to hand-building fake data cheaply in unit tests. The adapter
interface (`IAppInsightsQueryService`) returns plain, repo-owned `ReadModels`
(`AppInsightsExceptionGroupReadModel`, `AppInsightsTraceWarningReadModel`) —
`Nexus.Application/ReadModels` is already the repo's home for this kind of internal,
non-API-exposed projection shape (`AvailableInspectionSlotReadModel`, etc.). Only the
adapter implementation ever touches `LogsQueryClient`/`LogsQueryResult`; everything
upstream (fingerprinter, its tests) mocks `IAppInsightsQueryService` and works with
plain POCOs.

### NEW_REGRESSION detection — explicit read-then-branch, not an upsert trick

No raw-SQL `ON CONFLICT ... RETURNING (xmax = 0)` trick is needed or wanted — this
codebase has zero raw-SQL upsert usage; every write path is EF's read-then-mutate-then-
`Update()` (see `SasExpiryJob`, `IngestionJob`). The **Fingerprinter explicitly reads
before writing**, and the absence of a row *is* the new-fingerprint signal, computed
once per grouped poll-row, before any `Create()` call:

```csharp
// inside FingerprinterService, per grouped KQL row:
var hash = FingerprintHasher.ComputeExceptionHash(row.ProblemId);   // or ComputeTraceHash(normalized)
var existing = await _fingerprintRepository.GetByHashAsync(hash, ct);
var isNewFingerprint = existing is null;

if (isNewFingerprint)
{
    var fp = new Fingerprint { Id = FingerprintHasher.GenerateFingerprintId(hash), Hash = hash, /* ... */ };
    await _fingerprintRepository.Create(fp, ct);
    existing = fp;
}
else
{
    existing.TotalCount += row.Count;
    existing.LastSeen = row.LastSeen;
    _fingerprintRepository.Update(existing);
}

await _fingerprintRepository.AddOccurrence(existing.Id, windowFrom, row.Count, ...);
await _classifier.ClassifyAsync(existing, isNewFingerprint, row.Count, ct);
```

`isNewFingerprint` is passed as an **explicit parameter** into the rule classifier
(never re-derived from `category`/`classified_by` after the fact), so
`NEW_REGRESSION` can short-circuit deterministically per spec ("flag regardless of
type" — skips rule matching and the LLM call entirely when `isNewFingerprint == true`).
Safe against races because Hangfire recurring jobs run serially per schedule and the
cursor only advances after the whole poll commits — no concurrent-poller scenario to
guard against in MVP.

### Enum storage as strings, not ints

`FingerprintLevel`, `FingerprintCategory`, `ClassificationSource`, `GithubIssueStatus`
are stored via `HasConversion<string>()` — a deliberate deviation from `FileUpload`'s
int-conversion convention. Reasoning: this table is a human/DB-triage surface and
`category`/`github_status` values double as literal GitHub label text
(`category/DATA_QUALITY`, etc.) — readable strings in Postgres are worth the small
deviation from the int-enum norm elsewhere in the repo.

---

## Mechanics

- **KQL queries**: `private const string` fields directly in `AppInsightsQueryService.cs`,
  next to their one consumer — no precedent in the repo for embedded resource files
  (`.sql`, `.kql`, etc.) as build items. Time bounds are passed via the SDK's
  `QueryTimeRange` parameter to `QueryWorkspaceAsync`, **not** string-interpolated
  into the KQL text (sidesteps injection surface, matches idiomatic
  `Azure.Monitor.Query` usage better than the spec's literal `datetime({from})`
  placeholder syntax).

- **Hashing / id generation**: `Nexus.Application/Common/FingerprintHasher.cs`, a
  pure static class, no DI:
  ```csharp
  public static class FingerprintHasher
  {
      public static string ComputeExceptionHash(string problemId);      // SHA256("appinsights|exception|" + problemId)
      public static string ComputeTraceHash(string normalizedMessage);  // SHA256("appinsights|trace|" + normalizedMessage)
      public static string GenerateFingerprintId(string hashHex);       // "fp_" + hashHex[..6]
      public static string NormalizeMessage(string raw);
  }
  ```

- **Message normalization** (applied in this exact order inside `NormalizeMessage`):
  1. Strip quoted strings: `"[^"]*"` and `'[^']*'` → `{str}` (before GUID/number
     passes, so literals can't interfere with them).
  2. Strip GUIDs: `\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b` → `{guid}`.
  3. Strip number runs ≥3 digits: `\b\d{3,}\b` → `{n}`.
  4. Collapse repeated whitespace and `Trim()`.

  Case is deliberately left untouched (no `.ToLower()`) to avoid conflating
  genuinely distinct message templates — flagged as a tunable if it proves too
  strict/loose in practice. Numeric thresholds live as named constants in
  `FingerprintConstants`, not magic numbers.

- **Noise threshold / immediate-file policy**: a tiny pure static policy, not buried
  in the GitHub actor:
  ```csharp
  // Nexus.Application/Common/FingerprintFilingPolicy.cs
  public static class FingerprintFilingPolicy
  {
      public const int MinOccurrencesToFile = 3;
      public static bool ShouldFileIssue(int windowOccurrenceCount, bool isNewRegression)
          => isNewRegression || windowOccurrenceCount >= MinOccurrencesToFile;
  }
  ```
  Kept in `Common/` (alongside `Result.cs`) so the threshold rule is unit-testable
  with zero mocks, and `GitHubIssueService` stays focused on orchestration/I/O.

---

## Build order (7 phases, one PR each)

### Phase 1 — Schema, domain, repositories

- `Nexus.Domain/Enums/FingerprintEnums.cs` — `FingerprintLevel`, `FingerprintCategory`,
  `ClassificationSource`, `GithubIssueStatus`.
- `Nexus.Domain/Entities/Fingerprint.cs`, `FingerprintOccurrence.cs`, `IngestCursor.cs`
  — plain POCOs per schema, including `GithubLastCommentedAtUtc`.
- `Nexus.Infrastructure/Persistence/Configurations/FingerprintConfiguration.cs`,
  `FingerprintOccurrenceConfiguration.cs`, `IngestCursorConfiguration.cs` —
  `IEntityTypeConfiguration<T>`, snake_case tables, indexes: unique on `hash`,
  `(fingerprint_id, occurred_at)` on occurrences, `(github_status, level)` on
  fingerprints.
- `Nexus.Application/Interfaces/Repository/IFingerprintRepository.cs` :
  `IRepositoryBase<Fingerprint>` — `GetByHashAsync`, `GetByIdAsync(string)`,
  `AddOccurrence(...)`, `GetOpenAsync(level?, ct)`,
  `GetSparklineBucketsAsync(fingerprintId, bucketCount, ct)` (via
  `EF.Functions.DateTrunc`), `GetHourlyBaselineAsync(fingerprintId, ct)` (for
  `RECURRING_KNOWN` spike detection).
- `Nexus.Application/Interfaces/Repository/IIngestCursorRepository.cs` —
  `GetAsync(source, ct)`, `Upsert(source, lastPolledTo)`. Separate from
  `IFingerprintRepository` — it's a distinct aggregate root, not a child of
  `Fingerprint`.
- `Nexus.Infrastructure/Repositories/FingerprintRepository.cs`,
  `IngestCursorRepository.cs`.
- `Nexus.Infrastructure/Migrations/<timestamp>_AddFingerprintTables.cs` — single
  migration for all 3 tables including `github_last_commented_at`.
- `AppDbContext.cs` — add `DbSet<Fingerprint>`, `DbSet<FingerprintOccurrence>`,
  `DbSet<IngestCursor>`.
- Register repos in `Nexus.Application/Extensions/AppExtensions.cs`.

### Phase 2 — App Insights adapter + hashing/normalization

- `Nexus.Application/ReadModels/AppInsightsExceptionGroupReadModel.cs`,
  `AppInsightsTraceWarningReadModel.cs`.
- `Nexus.Application/Interfaces/IAppInsightsQueryService.cs` —
  `QueryExceptionGroupsAsync(from, to, ct)`, `QueryTraceWarningsAsync(from, to, ct)`.
- `Nexus.Application/Services/AppInsightsQueryService.cs` — wraps `LogsQueryClient`,
  holds the two KQL `const string`s, maps `LogsTable` rows → read models.
- `Nexus.Application/Settings/FingerprintIngestSettings.cs` — `WorkspaceId`,
  `PollWindowMinutes = 15`, `IngestionLagMinutes = 5`.
- `Nexus.Application/Common/FingerprintHasher.cs`.
- `Nexus.Application/Constants/FingerprintConstants.cs` — sparkline bucket count (7),
  baseline-spike multiplier (3), comment-throttle window (1h), min digit-run length (3).
- Register `LogsQueryClient` singleton in `InfrasExtensions.cs` (mirrors
  `BlobServiceClient`): `services.AddSingleton(sp => new LogsQueryClient(new DefaultAzureCredential()))`.
  Bind `FingerprintIngestSettings`.
- Tests: `Nexus.Tests/Unit/Application/FingerprintHasherTests.cs` (hash determinism,
  each normalization case, ordering edge cases).

### Phase 3 — Fingerprinter + Hangfire ingest job

- `Nexus.Application/Interfaces/Business/IFingerprinterService.cs` /
  `Nexus.Application/Services/FingerprinterService.cs` — read-then-branch upsert
  logic above; returns `(Fingerprint fingerprint, bool isNewFingerprint, int windowCount)`
  per processed row.
- `Nexus.Application/Interfaces/Business/IFingerprintIngestJob.cs` /
  `Nexus.Application/Services/FingerprintIngestJob.cs` — copies the `SasExpiryJob`
  template exactly: constructor-injects `IAppInsightsQueryService`,
  `IFingerprinterService`, `IIngestCursorRepository`, `IUnitOfWork`,
  `ILogger<FingerprintIngestJob>`. `ExecuteAsync()`: read cursor → compute
  `[from, now-5min)` window → run both KQL queries → run each row through
  `FingerprinterService` → advance cursor **only after** `_uow.SaveChanges()`
  commits the whole batch.
- Register in `AppExtensions.cs`; add
  `RecurringJob.AddOrUpdate<IFingerprintIngestJob>("fingerprint-ingest", job => job.ExecuteAsync(), "*/15 * * * *")`
  in `Program.cs`, alongside the existing `sas-expiry-check` registration.
- Tests: `FingerprinterServiceTests.cs` (new-vs-existing branching, count increments,
  occurrence creation), `FingerprintIngestJobTests.cs` (cursor advance-only-on-success,
  empty-window no-op — mirrors `IngestionJobTests` naming:
  `ExecuteAsync_WhenNoNewData_ShouldNotAdvanceCursor`, etc.).

### Phase 4 — Rule classifier + Python `/classify` contract + ownership router

- `Nexus.Application/Settings/FingerprintRoutingSettings.cs` + `OwnershipRule` +
  `OwnershipMatch` (see [Design decisions](#ownership-map--plain-optionst-binding-a-json-array)).
  Add section to `appsettings.json`, bind in `InfrasExtensions.cs`.
- Extend `AiServiceSettings.cs` with `Classify` and `Summarize` endpoint-path
  properties; add to `appsettings.json`.
- `Nexus.Application/Dtos/Requests/AiFingerprintClassifyRequest.cs` (snake_case:
  `exception_type`, `message_template`, `sample_trace`, `operation`),
  `Nexus.Application/Dtos/Responses/AiFingerprintClassifyResponse.cs` (`category`,
  `confidence`, `rationale`).
- `Nexus.Application/ReadModels/FingerprintClassificationResult.cs` (`Category`,
  `Confidence`, `Rationale`, `Source`).
- `Nexus.Application/Interfaces/IFingerprintAiService.cs` /
  `Nexus.Application/Services/FingerprintAiService.cs` — `ClassifyAsync(...)`,
  builds request/maps response exactly like `AiService`, throws
  `FingerprintAiServiceException` on failure (mirrors `AiServiceException`).
- `Nexus.Application/Interfaces/Business/IFingerprintClassifier.cs` /
  `Nexus.Application/Services/FingerprintRuleClassifier.cs` — rule table first
  (`DEPENDENCY_FAILURE`, `CONFIG_AUTH`, `DATA_QUALITY`, `PERFORMANCE` matchers;
  `RECURRING_KNOWN` via `GetHourlyBaselineAsync` + 3x multiplier; `NEW_REGRESSION`
  short-circuits on `isNewFingerprint == true` before any other rule runs); falls
  back to `IFingerprintAiService.ClassifyAsync` only when no rule matched. Sets
  `AutoFixEligible` from `FingerprintRoutingSettings.AutoFixAllowlistCategories` +
  `AutoFixDenylistNamespaces` (denylist always wins).
- `Nexus.Application/Interfaces/Business/IFingerprintRouter.cs` /
  `Nexus.Application/Services/FingerprintRouter.cs` — pure, first-match-wins over
  `Ownership`, falls back to `DefaultAssignee`. No LLM call, ever.
- Wire classifier + router into `FingerprinterService`/`FingerprintIngestJob`,
  immediately after upsert, before occurrence commit.
- Tests: `FingerprintRuleClassifierTests.cs` (each taxonomy rule; LLM fallback only
  when no rule matches; `NEW_REGRESSION` short-circuit verified via `Times.Never` on
  the AI mock), `FingerprintRouterTests.cs`, `FingerprintFilingPolicyTests.cs`.

### Phase 5 — GitHub actor (Octokit) + idempotency + Python `/summarize` contract

- `Nexus.Application/Settings/GitHubSettings.cs` — `Token`, `Owner`, `Repo`. Bind in
  `InfrasExtensions.cs`; placeholder section in `appsettings.json`; Key Vault in prod
  (same convention as `StripeSettings.SecretKey`).
- Register `Octokit.IGitHubClient` singleton in `InfrasExtensions.cs`, same style as
  the existing `IStripeClient` registration.
- Add `Octokit` NuGet package to `Nexus.Application.csproj`.
- `Nexus.Application/Dtos/Requests/AiFingerprintSummarizeRequest.cs` (nested
  snake_case fingerprint + last-5-occurrences), `AiFingerprintSummarizeResponse.cs`
  (`title`, `body`). Add `SummarizeAsync` to `IFingerprintAiService`.
- `Nexus.Application/ReadModels/FingerprintIssueContent.cs` (`Title`, `Body`).
- `Nexus.Application/Interfaces/Business/IGitHubIssueService.cs` /
  `Nexus.Application/Services/GitHubIssueService.cs`:
  - `ProcessFingerprintAsync(fp, windowOccurrenceCount, isNewRegression, ct)` — used
    by the ingest pipeline; implements all four idempotency branches: no issue +
    `ShouldFileIssue` → create; open issue → throttled count comment (via
    `GithubLastCommentedAtUtc`); closed issue reappears → reopen + "regressed"
    comment; below-threshold and not new → no-op.
  - `ForceFileIssueAsync(fp, ct)` — manual REST trigger; skips the noise threshold
    but still respects "already open" / "closed → reopen".
  - `AddAutoFixCandidateLabelAsync(fp, ct)` — the entire "agent loop" hook: adds
    `auto-fix-candidate` label if `fp.AutoFixEligible && fp.GithubIssueNumber != null`,
    else `Result.Conflict`.
  - Labels on create: `severity/error|warning`, `category/<category>`.
- Wire `ProcessFingerprintAsync` into `FingerprintIngestJob`, after
  classification/routing, per row.
- Tests: `GitHubIssueServiceTests.cs` — mock `IGitHubClient`'s issue surface +
  `IFingerprintAiService`, cover all four branches, verify comment throttle respects
  `GithubLastCommentedAtUtc`.

### Phase 6 — REST API for the (deferred) dashboard

- `Nexus.Application/Dtos/Responses/FingerprintListItemResponse.cs` (id, level,
  category, service, count, first/last seen, github status, 7-bucket sparkline),
  `FingerprintDetailResponse.cs` (+ sample_trace, occurrences summary),
  `FingerprintStatsResponse.cs` (`OpenErrors`, `OpenWarnings`, `IssuesAssignedToday`,
  `AgentPrsAwaitingReview` — this will read `0` until a future phase adds the
  GitHub-Actions agent loop that sets `GithubStatus = Pr`; not a bug in this plan).
- `Nexus.Application/Interfaces/Business/IFingerprintService.cs` /
  `Nexus.Application/Services/FingerprintQueryService.cs` — `GetListAsync(status?, level?, ct)`,
  `GetByIdAsync(id, ct)`, `FileIssueAsync(id, ct)` → `ForceFileIssueAsync`,
  `SendToAgentAsync(id, ct)` → `AddAutoFixCandidateLabelAsync`, `ResolveAsync(id, ct)`.
  All return `Result<T>`.
- `Nexus.Api/Controllers/FingerprintController.cs` : `AppControllerBase`,
  `[Route("api/fingerprints")]` — list/detail/file-issue/send-to-agent/resolve,
  following the standard `MapFailure` pattern (auth required — internal triage tool,
  no `[AllowAnonymous]`).
- `Nexus.Api/Controllers/FingerprintStatsController.cs` : `AppControllerBase`,
  `[Route("api/stats")]`.
- Register `IFingerprintService` in `AppExtensions.cs`.
- Tests: `FingerprintQueryServiceTests.cs` (each method, `Result` branches).

### Phase 7 — Config wiring + manual setup docs

- Full `appsettings.json` additions: `FingerprintIngestSettings`,
  `FingerprintRoutingSettings`, `GitHubSettings`, extended `AiServiceSettings.Classify`/
  `Summarize` — blank placeholders, matching existing convention.
- New `Docs/fingerprint-setup.md` documenting the manual, non-automatable follow-up:
  - Provisioning `DefaultAzureCredential` (managed identity in Azure, `az login` locally).
  - Granting **Log Analytics Reader** RBAC on the target App Insights workspace.
  - Obtaining the workspace GUID for `FingerprintIngestSettings.WorkspaceId`.
  - Generating a GitHub PAT with `repo` scope, storing it in Key Vault under the same
    naming convention as `StripeSettings:SecretKey`.

---

## Python HTTP contract

Both endpoints added to the existing `rec_brain` sidecar, authenticated the same way
as every other `AiService` call: header `X-API-Key: <AiServiceSettings.ApiKey>`.

### `POST {AiServiceSettings.BaseUrl}/{AiServiceSettings.Classify}`

```json
// request
{
  "exception_type": "System.Net.Http.HttpRequestException",
  "message_template": "Connection refused to {n}.{n}.{n}.{n}:{n}",
  "sample_trace": "at RagService.Client.SendAsync() ...",
  "operation": "RagService.QueryDocuments"
}
// response
{
  "category": "DEPENDENCY_FAILURE",
  "confidence": 0.87,
  "rationale": "Outbound HTTP timeout to an external service dependency."
}
```

`category` must be one of the six taxonomy values (`DEPENDENCY_FAILURE`,
`NEW_REGRESSION`, `RECURRING_KNOWN`, `CONFIG_AUTH`, `DATA_QUALITY`, `PERFORMANCE`).
The .NET mapping layer in `FingerprintAiService` is responsible for translating this
to the `FingerprintCategory` enum.

### `POST {AiServiceSettings.BaseUrl}/{AiServiceSettings.Summarize}`

```json
// request
{
  "fingerprint": {
    "id": "fp_a1b2c3",
    "level": "error",
    "category": "DEPENDENCY_FAILURE",
    "exception_type": "System.Net.Http.HttpRequestException",
    "message_template": "Connection refused to {n}.{n}.{n}.{n}:{n}",
    "operation": "RagService.QueryDocuments",
    "service_name": "rag-service",
    "total_count": 47,
    "first_seen": "2026-07-15T09:00:00Z",
    "last_seen": "2026-07-16T08:45:00Z"
  },
  "occurrences": [
    { "occurred_at": "2026-07-16T08:00:00Z", "occurrence_count": 12, "rendered_message": "Connection refused to 10.0.0.4:5432" }
  ]
  // up to 5 most recent occurrences
}
// response
{
  "title": "DependencyFailure: RagService.QueryDocuments connection refused (47x since Jul 15)",
  "body": "## Summary\n...\n## Fingerprint\n`fp_a1b2c3`\n..."
}
```

---

## Out of scope

- **React dashboard** — only the REST API it will consume (Phase 6) is built here.
- **Real Azure credential wiring** — `DefaultAzureCredential`, workspace RBAC grant,
  and the actual workspace ID are a manual setup step (`Docs/fingerprint-setup.md`
  from Phase 7), not executed/tested end-to-end in this plan. The adapter is built
  and unit-tested against fake data behind `IAppInsightsQueryService`.
- **GitHub Actions coding-agent workflow** — the spec itself calls this a post-MVP
  hook. Only the `auto-fix-candidate` label (`AddAutoFixCandidateLabelAsync`) is
  built; no workflow file, no PR-opening agent, no CI-retry logic.
- **GitHub webhooks** — no inbound endpoint for GitHub events (e.g. PR merged); until
  that exists, `github_status` never automatically reaches `pr`/`merged`, so
  `AgentPrsAwaitingReview` in `/api/stats` will read `0`.

---

## Verification

Each phase is independently buildable/testable:

```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Fingerprint"
```

Phase 1 is verified via `dotnet ef database update` applying the new migration
cleanly against a local Postgres instance. Phases 2–6 are verified via their unit
test suites (all external dependencies mocked — no real Azure/GitHub calls in CI).
End-to-end verification against real App Insights/GitHub happens only after the
manual setup in Phase 7's `Docs/fingerprint-setup.md` is complete, and is out of
scope for automated tests.
