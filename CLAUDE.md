# Nexus — Developer Reference

## Running the project

```bash
# Apply migrations
dotnet ef database update --project Nexus.Infrastructure --startup-project Nexus.Api

# Run the API
dotnet run --project Nexus.Api

# Run tests
dotnet test
```

Swagger: `https://localhost:7289/swagger`  
Hangfire dashboard: `/hangfire` (requires JWT Bearer token)

## Architecture conventions

### Identity resolution
Services receive `IUserContext` via DI and read `UserId` internally. **Never pass userId as a method parameter** from controllers. The only exception is background jobs (e.g. `InvoiceExtractionJob`, `IngestionJob`), which have no HTTP context and receive IDs from the queued job payload.

### Result pattern
All service methods return `Result<T>`. Use `Result<T>.Success(value)`, `Result<T>.NotFound(code, message)`, and `Result<T>.Conflict(code, message)`. Controllers map these to HTTP responses via a shared helper.

### Repository + Unit of Work
Repositories handle queries and staging changes; `IUnitOfWork.SaveChanges()` commits. Always call `SaveChanges` after staging mutations.

### Background jobs (Hangfire)
Enqueue via `IBackgroundJobClient.Enqueue<IJobInterface>(job => job.ExecuteAsync(...))`. Job classes live in `Nexus.Application/Services/`. Two current pipelines:
- **IngestionJob** — document ingestion (AI sidecar `/ingest`)
- **InvoiceExtractionJob** — invoice/receipt extraction (AI sidecar `/invoice-extract`)

Both update `FileUpload.IngestionStatus` (Queued → Processing → Completed/Failed).

### File upload flow
1. Client calls `POST /api/files/upload-url` with `FileName`, `ContentType`, `Purpose`
2. Server generates SAS URL and creates a `FileUpload` record (status: Pending)
3. Client uploads directly to Azure Blob Storage
4. Client calls `POST /api/files/confirm/{id}` — status moves to Completed, `IngestionStatus` set to Queued for non-General uploads
5. Azure Function blob trigger fires → calls internal endpoint → Hangfire job enqueued

### Upload purposes and containers
| Purpose | Container setting | Allowed file types | Downstream job |
|---|---|---|---|
| `General` | `ContainerName` | images, pdf, doc/docx | none |
| `Extraction` | `ExtractionContainerName` | images only | — |
| `Ingestion` | `IngestionContainerName` | pdf, doc/docx | `IngestionJob` |
| `Invoice` | `InvoiceContainerName` | images, pdf, doc/docx | `InvoiceExtractionJob` |

### Authentication

`AuthController` exposes `login`, `register`, `external-login`, `refresh`, and `logout` — all `[AllowAnonymous]`. `me` requires auth and reads the current user via `ITokenService.GetCurrentUser()`.

Refresh tokens are single-use and rotated on every refresh: `UserService.RefreshAsync` hashes the incoming token, looks it up via `IRefreshTokenRepository.GetByTokenHash`, marks it revoked, and issues a new access token + new refresh token.

**Reuse detection:** if the looked-up token is already `IsRevoked`, that's treated as a signal of token theft — a stolen, already-rotated token being replayed. `RefreshAsync` calls `IRefreshTokenRepository.RevokeAllForUser` to kill *every* active refresh token for that user, not just the one presented, then returns `Unauthorized`.

`LogoutAsync` revokes only the single presented refresh token (`NotFound` if it doesn't exist). Revoking an already-revoked token via logout is treated as idempotent success — unlike the reuse case in `RefreshAsync`, an already-revoked token seen by `LogoutAsync` is not a theft signal. Same underlying state (`IsRevoked`), different meaning depending on which flow observes it.

**Rate limiting:** `login`, `register`, and `refresh` are decorated with `[EnableRateLimiting("<policy>")]`. Policies are per-IP sliding windows configured via `RateLimitSettings` (`appsettings.json`) and wired up by `AddNexusRateLimiting` (`Nexus.Api/Extensions/RateLimitingExtensions.cs`). A 429 returns the shared `ErrorResponse` shape with a `Retry-After` header.

`Program.cs` calls `app.UseForwardedHeaders(...)` (`ForwardedHeaders.XForwardedFor`, `ForwardLimit = 1`) before `app.UseRateLimiter()` — App Service sits in front of the app as a reverse proxy, so without it every request appears to come from App Service's edge IP and all clients share one rate-limit bucket. `ForwardLimit = 1` trusts exactly the one hop App Service appends; `KnownProxies`/`KnownNetworks` isn't usable since App Service's edge IPs aren't a fixed, enumerable set.

### Azure Functions — blob trigger pipeline

The two Azure Functions (`BlobIngestionFunction`, `BlobInvoiceExtractionFunction`) in `Nexus.Functions` use **`[EventGridTrigger]`**, not `[BlobTrigger]`. This is intentional — do not switch them back.

#### Why not BlobTrigger with EventGrid source?

`BlobTrigger` with `Source = BlobTriggerSource.EventGrid` is broken on the **Flex Consumption** plan. When the blob extension registers its webhook with Event Grid, it omits the `?code=` system key from the URL. Event Grid's validation callback therefore gets a 401, and the subscription never activates. Even when the subscription was manually forced through (via REST API), the host accepted incoming events (HTTP 202) but the function body never executed. The root cause was never fully isolated — possibly a Flex Consumption cold-start interaction with the blob trigger processing queue. Switching to `[EventGridTrigger]` resolved it completely.

#### How it works now

1. A blob is written to `rec-dev-ingestion` or `rec-dev-invoices` in `nexusragdevstg`.
2. The Event Grid system topic `egst-dev-ingest-storage` fires a `Microsoft.Storage.BlobCreated` event.
3. An Event Grid subscription delivers it via webhook to the function app's `/runtime/webhooks/EventGrid?functionName=<FunctionName>&code=<eventgrid_extension key>`.
4. The function extracts the blob name from `eventGridEvent.Subject` (format: `/blobServices/default/containers/{container}/blobs/{blobName}`).
5. It calls the Nexus API internal endpoint (`/api/internal/documents/ingest` or `/api/internal/invoices/extract`), which enqueues the Hangfire job.

#### Manual infrastructure (not in code / not auto-created)

The Event Grid subscriptions must be created manually via **REST API** — the Azure CLI mangles the `==` at the end of the base64 system key, causing a 401 during webhook validation.

- **System topic:** `egst-dev-ingest-storage` on storage account `nexusragdevstg` (rg-nexus-dev)
- **Subscription 1:** `blob-ingestion-trigger`
  - Filter: `Microsoft.Storage.BlobCreated`, subject starts with `/blobServices/default/containers/rec-dev-ingestion/`
  - Endpoint: `https://<func-host>/runtime/webhooks/EventGrid?functionName=BlobIngestionFunction&code=<eventgrid_extension key>`
- **Subscription 2:** `blob-invoice-trigger`
  - Filter: `Microsoft.Storage.BlobCreated`, subject starts with `/blobServices/default/containers/rec-dev-invoices/`
  - Endpoint: `https://<func-host>/runtime/webhooks/EventGrid?functionName=BlobInvoiceExtractionFunction&code=<eventgrid_extension key>`

The `eventgrid_extension` system key can be retrieved from the function app's host keys in the Azure portal. URL-encode the key (`==` → `%3D%3D`) when constructing the webhook URL via REST API.

### AI sidecar (rec_brain)
Base URL and endpoint paths live under `AiServiceSettings`. All requests require `X-API-Key` header. `AiService` wraps the chat/ingestion/invoice calls and `FingerprintAiService` wraps `/classify`; both build their `RequestBuilderOptions` through the shared `AiRequestOptionsFactory` (`Nexus.Application/Common/`) — don't re-add per-service private builder methods. Raw Python response DTOs use snake_case fields (e.g. `AiInvoiceExtractionResponse`), which are mapped to PascalCase application DTOs before returning.

### Database
PostgreSQL via EF Core with Npgsql snake_case naming convention. Migrations are in `Nexus.Infrastructure/Migrations/`. Add migrations with:
```bash
dotnet ef migrations add <Name> --project Nexus.Infrastructure --startup-project Nexus.Api
```

### Configuration / Settings
Every settings class bound via `services.Configure<T>(config.GetSection(nameof(T)))` needs a matching section in **both** `appsettings.json` (blank placeholders) **and** `appsettings.Development.json` (real local-dev values, or blanks if none apply yet). `appsettings.Development.json` overrides `appsettings.json` at runtime in the Development environment — a section only added to the base file still binds, but silently with blank/default values locally, which can mask a missing-config bug until deploy. Add the new section to `appsettings.Development.json` in the same PR that introduces the settings class, not as a follow-up.

### Validation
FluentValidation validators are co-located with their request DTOs (same file). Registered automatically via the API project's DI extensions.

### Enums
Domain enums live in `Nexus.Domain/Enums/`. Grouped by domain:
- `FileUploadEnums.cs` — `UploadPurpose`, `UploadStatus`, `IngestionStatus`
- `InvoiceEnums.cs` — `DocumentType` (Invoice, Receipt)
- `PropertyEnums.cs` — property and listing enums
- `FingerprintEnums.cs` — `FingerprintLevel`, `FingerprintCategory`, `ClassificationSource`,
  `GithubIssueStatus`. Stored via `HasConversion<string>()`, not the repo's usual
  `HasConversion<int>()` — see [Fingerprint feature](#fingerprint-feature-log-triage-system).

### Fingerprint feature (log-triage system)

Full design lives in `Docs/fingerprint-implementation-plan.md`; two companion docs:
`Docs/fingerprint-spec.md` (cross-codebase spec updated to as-built, incl. the React
dashboard UI spec) and `Docs/fingerprint-api-contract.md` (**authoritative REST
contract for the frontend** — if the docs disagree on an API detail, the contract
wins). The system — poll App Insights,
dedupe telemetry into "fingerprints", classify (rules → LLM fallback), route to a
GitHub assignee, file/update GitHub issues idempotently. Built phase-by-phase, one
PR per phase; each phase's plan is worked out fresh against the doc before coding.
**Phases 1–7 are done**: schema/domain/repositories, the App Insights adapter +
hashing/normalization, the `FingerprinterService`/`FingerprintIngestJob` pair that
polls App Insights on a 15-minute Hangfire recurring job and upserts `Fingerprint`/
`FingerprintOccurrence` rows by hash, the rule classifier + Python `/classify`
contract + ownership router, the GitHub actor (`GitHubIssueService`, Octokit)
+ Python `/summarize` contract, the REST API for the future dashboard, and the
telemetry emission side (Phase 7). Manual Azure/GitHub setup steps live in
`Docs/fingerprint-setup.md`. `Program.cs` registers the `fingerprint-ingest`
recurring job **only when `FingerprintIngestSettings:WorkspaceId` is non-blank**
(and calls `RemoveIfExists` otherwise, since Hangfire persists recurring jobs in
storage — a previously registered job would keep firing after the setting is
blanked). So a server without fingerprint settings simply doesn't poll. The Phase 6 read
endpoints and `/api/stats` only touch Postgres and work fine without any fingerprint
settings; the action endpoints need `GitHubSettings` to succeed.

**Phase 4** wires classification into `FingerprinterService`, immediately after the
upsert `if/else` and before `AddOccurrenceAsync`, via `IFingerprintClassifier`
(`FingerprintRuleClassifierService`): `NEW_REGRESSION` short-circuits on `isNewFingerprint`
before any repository/AI call; then fixed-priority content rules match
`ExceptionType`/`MessageTemplate` keyword tables (`DependencyFailure` >
`ConfigAuth` > `DataQuality` > `Performance`); then `RECURRING_KNOWN` compares the
window count against `GetHourlyBaselineAsync(...) * FingerprintConstants.MinSpikeMultiplier`
(that baseline averages the fingerprint's *whole* occurrence history, not a recent
window — an accepted MVP tradeoff, not a bug); only if nothing matched **and**
`fingerprint.Category is null` (classify once, sticky — a later rule can still
override a stale LLM call, but a bad LLM result isn't retried) does it fall back to
`IFingerprintAiService.ClassifyAsync`. A `FingerprintAiServiceException` from that
call is caught and swallowed *inside the classifier*, not propagated — one flaky AI
call must not abort the rest of the poll batch, so the fingerprint is just left
unclassified and retried next poll.

`AutoFixEligible` is computed by every branch that sets a category, gated on
`AutoFixAllowlistCategories`/`AutoFixDenylistNamespaces` (`FingerprintRoutingSettings`)
and matched against `row.ProblemId` — the only namespace-shaped string available
(e.g. `"Nexus.Application.Services.InvoiceExtractionJob!ExecuteAsync"`), passed as
an extra classifier parameter since it's never persisted on `Fingerprint` itself.
Trace/warning-origin rows have no `ProblemId` at all, so `problemId is null` **fails
closed** — trace-origin fingerprints can never be `AutoFixEligible` in Phase 4.
`FingerprintCategoryWireFormat` (`Common/`) is the one place that converts between
the enum's PascalCase C# names and the SCREAMING_SNAKE_CASE strings the Python
contract / GitHub labels / `AutoFixAllowlistCategories` all use.

The `/classify` response contract is `category`/`confidence`/`reason`
(`AiFingerprintClassifyResponse`; the explanation field was renamed from
`rationale` to `reason`, and the rec_brain Python side has been updated to
send `"reason"` as well — both sides now match).

**Phase 5** adds the GitHub actor: `IGitHubIssueService`/`GitHubIssueService`
(Octokit; `IGitHubClient` registered singleton in `InfrasExtensions`, credentials
skipped when `GitHubSettings.Token` is blank so local boot doesn't crash).
`FingerprintIngestJob` calls `ProcessFingerprintAsync(fp, windowCount, isNew, ct)`
per row after the fingerprinter tuple returns; this is where the Phase 4-built
`FingerprintRouter` (issue assignee) and `FingerprintFilingPolicy` (noise
threshold) finally get their consumers. Four idempotency branches keyed off
`GithubStatus`: `None` + `ShouldFileIssue` → create (labels `severity/<level>`,
`category/<WIRE>`, assignee from router, body from `/summarize` with
`## Suggested Fix` appended only when `AutoFixEligible`); `Open` → count comment
throttled via `GithubLastCommentedAtUtc` (min 1h); `Closed` → reopen + "Regressed"
comment; `Pr`/`Merged`/below-threshold → no-op. All GitHub/AI failures are
swallowed inside `ProcessFingerprintAsync` (same batch-safety rationale as the
classifier); `ForceFileIssueAsync`/`AddAutoFixCandidateLabelAsync` (for Phase 6's
REST API) do *not* swallow and return `Result<Fingerprint>` with `Conflict` for
already-open / not-eligible / no-issue cases. `GitHubIssueService` calls
`_uow.SaveChanges()` immediately after each GitHub side effect rather than
waiting for the job's batch commit — a crash between issue creation and the batch
commit would lose the issue number and file a duplicate next poll.

**Commit ordering is load-bearing.** `FingerprintIngestJob` runs in three strict
phases: stage every exception and trace row through the fingerprinter, collecting
`PendingGitHubAction` records; **one** `SaveChanges` covering all fingerprints, all
occurrences, *and* the cursor; then `ProcessPendingGitHubActionsAsync`. The actor
must never run before that commit. Two things break if it does: a brand-new
fingerprint is still `Added`, so the actor's `_fingerprintRepository.Update(...)`
moves it to `Modified` and its commit emits an UPDATE against a row that was never
inserted, failing `fk_fingerprint_occurrences_fingerprints_fingerprint_id` on the
occurrence staged beside it; and the GitHub issue gets created before the
fingerprint is durable, so any failure in between leaves an issue with no
fingerprint behind it — which, because `ProcessFingerprintAsync` swallows
exceptions, is re-filed on every later poll. Observed live: 9 orphaned issues in one
afternoon. Guarded by
`ExecuteAsync_ShouldCommitTheWholeBatchBeforeInvokingTheGitHubActor`.

This replaced an earlier per-row `PersistBeforeSideEffectAsync()` commit. A single
`SaveChangesAsync` is already one transaction, so no explicit `BeginTransaction` is
needed. **The cursor advances inside that same commit deliberately**: a committed
window is never re-polled, which is what makes Hangfire's retry safe. Re-polling is
*not* idempotent for counters — `UpdateFingerprint` re-runs `TotalCount += count`
and `AddOccurrenceAsync` inserts a duplicate row, since `(FingerprintId, OccurredAt)`
is a plain `HasIndex`, not `IsUnique` — so a partially-committed batch used to
inflate sparklines and the spike baseline on every retry. Accepted tradeoff: if the
process dies between the commit and the GitHub loop, that batch's issues are never
filed. Exposure is low (the actor catches everything, so only process death reaches
it) and it self-heals for any recurring error, since the fingerprint keeps
`GithubStatus = None` and files normally next time it appears.

**The missed-filing retry** (`RetryMissedIssueFilingsAsync`) runs after the batch's own
GitHub loop and closes the gap that the in-batch cursor creates. A fingerprint can
only sit at `GithubStatus.None` *after creation* if its filing failed — the poll died
before the actor, GitHub errored, or the token was blank — because
`FingerprintFilingPolicy.ShouldFileIssue` is `isNewRegression || windowCount >= 3` and
`isNew` is true on first sighting, so the policy always approves a brand-new
fingerprint. The cursor has advanced past its window, so nothing re-examines it unless
the same error recurs. The retry re-reads `None` rows via
`IFingerprintRepository.GetUnfiledSinceAsync` (tracked, unlike the dashboard's
`AsNoTracking` `GetListAsync`, because the actor calls `Update`/`SaveChanges` on
them) and passes **`isNewRegression: true`** — deliberately bypassing the threshold,
since these already earned a "file it" and re-applying it would suppress them twice.
Ids already in this batch's `pending` are excluded: they are still `None` until the
actor files them, so the query returns them too, and processing them twice would file
an issue and immediately comment on it. Bounded by
`MissedIssueLookbackHours` (default 24, `<= 0` disables) and `MaxMissedIssueRetriesPerRun`
(default 25) — without the time bound it would keep retrying `FingerprintSeed`'s
mock `None` rows and every failure accumulated against a blank local token. It also
runs on the no-new-events path, since a quiet window is exactly when a backlog should
drain. Accepted gap: `None` means *the DB has no issue number*, not *no issue exists*;
if `CreateIssueAsync` reached GitHub but its commit failed, the retry files a
duplicate. Closing that would need a `fingerprint/<id>` label on every issue plus a
GitHub search before filing — deferred until duplicates are actually observed.

**Several source rows routinely resolve to one fingerprint**, so `FingerprintIngestJob`
runs both query results through its private `MergeByHash` overloads *before* staging.
The App Insights queries group by keys finer than the hash: the trace KQL groups by raw
`Message` while `NormalizeMessage` runs afterward in C#, and `ComputeExceptionHash` uses
only `severity|problemId` while the exception KQL also groups by `Operation`/
`ServiceName`. So `Blob "a.pdf" not found` and `Blob "b.pdf" not found` arrive as two
rows and are one fingerprint. Merging up front is load-bearing three ways: the
fingerprinter never tries to stage two `Fingerprint`s with the same deterministic Id
(EF rejects that **at `Add`**, not at `SaveChanges` — verified on EF Core 8.0.11 — which
kills the batch and freezes the cursor through all 10 retries); exactly one
`FingerprintOccurrence` row is written per window instead of one per source row, which
keeps sparklines and `GetHourlyBaselineAsync` honest; and the count reaching
`ShouldFileIssue`/the spike multiplier is the window's real total rather than one slice.
Merging is why `GetByHashAsync` can stay a plain query and why no batch-scoped identity
map is needed anywhere.

Relatedly, **`FingerprinterService.UpdateFingerprint` mutates the entity without calling
`_fingerprintRepository.Update`** — verified against EF Core 8.0.11, `DbSet.Update` on an
entity still `Added` from this batch flips the entry to `Modified` and `SaveChanges` then
emits an UPDATE for a row that was never inserted. `GetByHashAsync` only ever returns
tracked entities, so change tracking persists the mutations on its own;
`FingerprintRuleClassifierService` has always relied on the same thing for
`Category`/`AutoFixEligible`. Don't "restore" the `Update` call. The `GitHubIssueService`
`Update` calls are fine because they all run after the commit, when nothing is `Added`.

**Phase 6** adds the REST API: `FingerprintQueryService` (implements
`IFingerprintService`) behind `FingerprintController` (`api/fingerprints` —
list with `?status=`/`?level=` filters, detail, `file-issue`/`send-to-agent`/
`resolve` actions) and `FingerprintStatsController` (`api/stats`). The action
endpoints load the fingerprint (404 if missing) and delegate to Phase 5's
`ForceFileIssueAsync`/`AddAutoFixCandidateLabelAsync`/the new `CloseIssueAsync`,
propagating their `Conflict` results as 409s. **Resolve requires an existing
GitHub issue** — `CloseIssueAsync` closes the open issue via Octokit and sets
`GithubStatus = Closed`; a fingerprint with no issue gets `Conflict("NoGithubIssue")`
rather than a status flip (keeps `Closed` meaning "a real issue was closed", so the
ingest job's reopen-on-regression branch stays safe; a mute/dismiss feature would be
a separate decision). The list/detail responses expose `githubIssueUrl`, **derived not stored** — `Fingerprint`
persists only `GithubIssueNumber`, and `GitHubIssueUrlBuilder` (`Common/`) composes
`https://github.com/{Owner}/{Repo}/issues/{n}` from `GitHubSettings` on each read, so a
repository rename can't strand a stale link. It returns null when there's no issue
number *or* no configured owner/repo — without that second guard a local server with
blank `GitHubSettings` would advertise `https://github.com//issues/123`.
`GithubIssueFiledAtUtc` (nullable, set in `CreateIssueAsync`,
migration `AddGithubIssueFiledAtToFingerprints`) exists so `IssuesAssignedToday` in
`/api/stats` is an accurate "filed since UTC midnight" count instead of an
`UpdatedAtUtc` proxy that comment-bumps would inflate. Known accepted tradeoff:
the list endpoint fetches sparklines one query per fingerprint (N+1) — fine at
triage-dashboard scale, batch it only if lists reach hundreds of rows.

**Phase 7** adds the telemetry write side: `Nexus.Api` uses the **Azure Monitor
OpenTelemetry distro** (`Azure.Monitor.OpenTelemetry.AspNetCore`), wired in
`TelemetryExtensions.AddNexusTelemetry` — registration is **skipped entirely when
`APPLICATIONINSIGHTS_CONNECTION_STRING` is blank** (same convention as the blank
GitHub token), so local boot never needs Azure. Cloud role name is fixed there as
`nexus-api`; Postgres dependency tracing comes from `Npgsql.OpenTelemetry`
(`AddNpgsql()`). Serilog is registered as an **`ILoggerProvider`**
(`builder.Logging.ClearProviders()` + `AddSerilog(Log.Logger)`), **not** via
`builder.Host.UseSerilog(...)`. This is load-bearing and verified against live
telemetry: routing logs through Serilog's `writeToProviders` bridge hands the
Azure Monitor exporter an opaque state object, so **every trace landed with
`SeverityLevel = 0` and no `CategoryName`** — the fingerprint warning query
(`SeverityLevel == 2`) matched nothing at all. Registering Serilog as a provider
keeps MEL as the logger factory, so OpenTelemetry receives records with level and
category intact. Don't "simplify" this back to `UseSerilog`. Because MEL now
filters first, the `Logging:LogLevel` section governs what reaches App Insights
(Serilog's own `MinimumLevel` still gates its Console/File sinks) — keep the two
sections in sync. **Do not enable the App Service codeless Application Insights toggle**
— it double-instruments alongside the SDK. The ingest KQL in
`AppInsightsQueryService` is written in **Log Analytics workspace schema**
(`AppExceptions`/`AppTraces`, `TimeGenerated`, `ProblemId`, `AppRoleName`) because
`QueryWorkspaceAsync` queries the workspace, where classic resource-scope names
(`exceptions`/`traces`, `problemId`, `cloud_RoleName`) don't resolve — don't
"fix" it back to classic schema. Manual Azure setup (resource, RBAC, app
settings) is documented in `Docs/fingerprint-setup.md`. Don't configure OTel
`SamplingRatio` — sampling silently deflates fingerprint counts/spike detection.

**Mock data for UI development**: `dotnet run --project tools/FingerprintSeed` — a
standalone console seeder mirroring `tools/DbSeedTemp` (direct `AppDbContext`, own
`appsettings.Development.json`, deliberately **not** in `Nexus.sln`, same as
DbSeedTemp). Truncates only `fingerprints`/`fingerprint_occurrences` (never
`ingest_cursor` — that belongs to the real pipeline) and inserts 12 fingerprints
with full UI-state coverage: every `GithubStatus` (incl. `Pr`/`Merged`, which the
pipeline can never produce locally without a real GitHub token), all categories
plus unclassified/null-enrichment rows, and varied sparkline shapes. Ids/hashes
come from the real `FingerprintHasher`; timestamps are relative to run time, so
rerun it whenever sparklines need refreshing (idempotent wipe-and-reseed). Against
this data `/api/stats` reads `openErrors: 6, openWarnings: 4, issuesAssignedToday: 2,
agentPrsAwaitingReview: 1`. The seeder's config supports environment-variable
override — set `ConnectionStrings__DefaultConnection` to point it at another DB
(e.g. the Azure dev DB, whose connection string lives in Key Vault
`kv-my-nexus-dev`, secret `ConnectionStrings--DefaultConnection`) without touching
the committed `appsettings.Development.json` (which stays localhost). It prints
`Target: <host>` before wiping so the destination is always visible.

`FingerprintOccurrence` and `IngestCursor` each get their own repository
(`FingerprintOccurrenceRepository`, `IngestCursorRepository`) instead of a bespoke
method bolted onto `IFingerprintRepository` — `RepositoryBase<T>`'s generic
`Create`/`Update` are bound to one `T` per class, so a child/unrelated entity can't
reuse another aggregate's repository to stage its own writes. `IngestCursorRepository`
only adds a custom `GetAsync(source, ct)` lookup on top of the inherited `Create`/
`Update`; the create-vs-update branching for advancing the cursor lives in
`FingerprintIngestJob.AdvanceCursorAsync` (reusing the `IngestCursor` it already
fetched at the top of `ExecuteAsync`), not the repository — repositories here stay
limited to persistence primitives per [Repository + Unit of Work](#repository--unit-of-work).

Two repo-convention deviations, both deliberate:
- **`Fingerprint.Id` is this repo's first string primary key** (`fp_` + 8 hex chars,
  `HasMaxLength(16)`, `ValueGeneratedNever()`) — every other entity uses `Guid` or
  `int`. Deterministic id (re-derived from a hash, no DB round-trip) is what makes
  ingest upsert idempotent under retry.
- **Fingerprint's 4 enums are stored as `HasConversion<string>()`**, not this repo's
  normal int-enum convention (see `20260527032232_EnumConversionsToInt.cs`, which
  moved everything else *to* ints). Reasoning: `category`/`github_status` values
  double as literal GitHub label text and this is a human/DB-triage surface —
  readable strings in Postgres are worth the deviation here specifically.

**`EF.Functions.DateTrunc` is not available** in the installed
`Npgsql.EntityFrameworkCore.PostgreSQL` version (8.0.11) — confirmed by checking the
package's XML doc comments, not just a compile guess. `FingerprintRepository`'s
`GetSparklineBucketsAsync`/`GetHourlyBaselineAsync` group by extracted
`Year`/`Month`/`Day`/`Hour` components instead (translates reliably via standard
EF Core member translation) and reconstruct the bucket `DateTimeOffset` client-side
after materializing. Don't reach for `DateTrunc` here without re-checking the
installed package version first.

Phase 1 has no dedicated unit tests — pure schema/plumbing (entities, EF configs,
repositories, DI wiring), no branching/calculation logic to test in isolation, and
this repo has no precedent for testing repositories or `IEntityTypeConfiguration<T>`
classes. Its correctness check is the migration itself (`dotnet ef migrations add` /
`database update` against local Postgres). Tests start appearing from Phase 2
(`FingerprintHasherTests.cs`) once there's actual logic to unit test.

## Testing
Unit tests live in `Nexus.Tests/Unit/`. Tests use xUnit with Moq. Mock all external dependencies (repositories, AI service, blob storage). The test naming convention is `MethodName_Condition_ExpectedResult`.
