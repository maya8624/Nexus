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
Base URL and endpoint paths live under `AiServiceSettings`. All requests require `X-API-Key` header. `AiService` wraps all calls; raw Python response DTOs use snake_case fields (e.g. `AiInvoiceExtractionResponse`), which are mapped to PascalCase application DTOs before returning.

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

Full design lives in `Docs/fingerprint-implementation-plan.md` — poll App Insights,
dedupe telemetry into "fingerprints", classify (rules → LLM fallback), route to a
GitHub assignee, file/update GitHub issues idempotently. Built phase-by-phase, one
PR per phase; each phase's plan is worked out fresh against the doc before coding.
**Phases 1–4 are done**: schema/domain/repositories, the App Insights adapter +
hashing/normalization, the `FingerprinterService`/`FingerprintIngestJob` pair that
polls App Insights on a 15-minute Hangfire recurring job and upserts `Fingerprint`/
`FingerprintOccurrence` rows by hash, and the rule classifier + Python `/classify`
contract + ownership router. Later phases (GitHub actor, REST API, routing/GitHub
config) are not yet built.

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
call must not abort the whole poll batch's single end-of-job `SaveChanges`, so the
fingerprint is just left unclassified and retried next poll.

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

`IFingerprintRouter`/`FingerprintRouter` and `FingerprintFilingPolicy` are both
built, unit-tested, and registered in DI in Phase 4 but **not called** from
`FingerprinterService`/`FingerprintIngestJob` — neither has a real consumer until
Phase 5's GitHub actor exists. Don't treat their absence from the ingest pipeline as
a missed wiring step; it's deliberate.

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
