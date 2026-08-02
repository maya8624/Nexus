# Fingerprint feature — manual Azure/GitHub setup (Phase 7)

The code side of Phase 7 is built: `Nexus.Api` emits telemetry to Application
Insights via the **Azure Monitor OpenTelemetry distro** (`AddNexusTelemetry` in
`Nexus.Api/Extensions/TelemetryExtensions.cs`), and the fingerprint ingest KQL
(`AppInsightsQueryService`) reads it back from the Log Analytics workspace. This
doc is the remaining **manual** setup — none of it is created by code or CI.

Everything is **dormant by default**:

- Blank `APPLICATIONINSIGHTS_CONNECTION_STRING` → `AddNexusTelemetry` registers
  nothing; the app boots and runs with Serilog Console/File only.
- Blank `FingerprintIngestSettings:WorkspaceId` → `Program.cs` skips registering
  the `fingerprint-ingest` recurring job (and `RemoveIfExists` deregisters a
  previously persisted one).

So each step below can be done (and rolled back) independently.

---

## 1. Application Insights resource

Telemetry needs a **workspace-based** Application Insights resource (one backed
by a Log Analytics workspace). The ingest queries run at *workspace* scope
against `AppExceptions`/`AppTraces` — a classic (non-workspace) resource has no
such tables, and ingest would silently return nothing.

**Decision (2026-08):** a dedicated shared setup in `rg-nexus-dev` — Log
Analytics workspace **`log-nexus-dev`** + workspace-based App Insights resource
**`appi-nexus-dev`** — used by *both* `nexus-api` and `func-rec-ingest-dev`.
Sharing one workspace means fingerprint triage covers both services — the
ingest queries don't filter by role name, they group by it.

The Functions app previously reported to the auto-created `func-rec-ingest-dev`
App Insights resource, backed by the subscription's default workspace
(`DefaultWorkspace-…-EAU` in `DefaultResourceGroup-EAU`) — deliberately
abandoned in favor of a workspace we own; repoint the Function App's
`APPLICATIONINSIGHTS_CONNECTION_STRING` at `appi-nexus-dev`. Its historical
telemetry stays queryable in the old resource until retention expires; the old
resource can be deleted after that.

Record two values:

| Value | Where to find it | Used for |
| --- | --- | --- |
| **Connection string** | App Insights resource → Overview | `APPLICATIONINSIGHTS_CONNECTION_STRING` app setting (step 2) |
| **Workspace ID** (a GUID) | The *Log Analytics workspace* → Overview → "Workspace ID" | `FingerprintIngestSettings__WorkspaceId` app setting (step 4) |

> ⚠️ The Workspace ID is the Log Analytics workspace GUID — **not** the App
> Insights instrumentation key, and **not** an ARM resource id.

## 2. App Service `nexus-api` — telemetry app settings

Portal → App Service `nexus-api` → Environment variables / Configuration:

1. Add `APPLICATIONINSIGHTS_CONNECTION_STRING` = the connection string from step 1.
2. **Leave the codeless agent OFF**: do *not* press "Enable" on the App Service's
   Application Insights blade, and delete the `ApplicationInsightsAgent_EXTENSION_VERSION`
   app setting if it exists. The SDK in code replaces it; running both
   double-instruments the app and would double fingerprint occurrence counts.
   (The blade will nag that App Insights "isn't enabled" — that's fine; it only
   means the *agent* is off.)

The cloud role name is fixed in code as `nexus-api`
(`TelemetryExtensions.ServiceName`) — it becomes `AppRoleName` in the workspace
tables, which the exception KQL groups by. Don't rename it casually: renaming
changes nothing about fingerprint identity (hashes don't include it) but breaks
any saved queries/dashboards filtering on it.

**Sampling:** the distro default is no trace sampling and logs are never
sampled. Do not configure `SamplingRatio` — sampling would silently deflate
fingerprint window counts and the `RECURRING_KNOWN` spike detection.

**What flows:** requests/dependencies/unhandled exceptions are auto-collected;
Serilog is registered as an `ILoggerProvider` so OpenTelemetry receives log
records straight from Microsoft.Extensions.Logging — logged warnings land in
`AppTraces` with `SeverityLevel = 2` and logged exceptions in `AppExceptions`
with a populated `ProblemId`. The `Logging:LogLevel` section (not Serilog's
`MinimumLevel`) gates what reaches App Insights, since MEL filters first.

> ⚠️ Do not switch this to `builder.Host.UseSerilog(..., writeToProviders: true)`.
> That bridge flattens every trace to `SeverityLevel = 0`, silently breaking the
> warning half of the pipeline. Verified against live telemetry, 2026-08-01.

**Local development:** `DefaultAzureCredential` hard-fails on
`ManagedIdentityCredential` when probing IMDS off-Azure instead of falling
through to your `az login`. `launchSettings.json` sets
`AZURE_TOKEN_CREDENTIALS=dev` to restrict the chain to developer credentials —
without it the ingest job throws `AuthenticationFailedException` every run.

## 3. Managed identity + RBAC (read side)

The ingest job authenticates with `DefaultAzureCredential`
(`LogsQueryClient` registration in `InfrasExtensions.cs`).

1. App Service `nexus-api` → Identity → **System assigned** → On.
2. Log Analytics workspace → Access control (IAM) → Add role assignment →
   **Log Analytics Reader** → the `nexus-api` managed identity.
3. RBAC propagation can take ~15 minutes; a `403` from the first job run right
   after granting is usually just that.

Local dev: `az login` with an account that has the same **Log Analytics Reader**
role on the workspace.

## 4. Activate the ingest job

App Service `nexus-api` app settings:

- `FingerprintIngestSettings__WorkspaceId` = the Workspace ID GUID from step 1
  (double-underscore = nested config key as an environment variable).

Restart the app. On boot, `Program.cs` sees the non-blank WorkspaceId and
registers the `fingerprint-ingest` recurring job (every 15 min). First-ever run
looks back 60 minutes (`InitialLookbackMinutes`); the cursor advances from there.

## 5. GitHub actor (issue filing)

Same conventions as the other secrets:

- Create a GitHub PAT with `repo` scope.
- Key Vault `kv-my-nexus-dev` → secret **`GitHubSettings--Token`** (the `--`
  separator is Key Vault's nested-key convention, same as
  `ConnectionStrings--DefaultConnection` / `StripeSettings--SecretKey`; it flows
  in through the `KeyVaultUrl` config source in `Program.cs`).
- Plain app settings: `GitHubSettings__Owner`, `GitHubSettings__Repo`,
  plus `FingerprintRoutingSettings__DefaultAssignee` (and the
  `Ownership`/allowlist arrays if used — array elements are
  `FingerprintRoutingSettings__Ownership__0__...` style).

With a blank token the GitHub client registration is skipped (local boot safe);
the read endpoints and `/api/stats` work regardless.

## 6. Verification checklist (in order)

1. **Telemetry out** — after step 2 + deploy: hit the API, trigger one
   `LogWarning` and one unhandled exception (any 500). App Insights →
   Transaction search should show the request, the warning trace, and the
   exception (allow 1–3 min ingestion latency).
2. **Workspace schema** — Log Analytics workspace → Logs: run the two queries
   from `AppInsightsQueryService` verbatim. Both must resolve, rows must show
   `ServiceName == "nexus-api"`, and — critical — **`ProblemId` must be
   non-empty** for the exception rows (it's the fingerprint key). If it's ever
   empty for OTel-ingested exceptions, stop and revisit the exception query
   before activating ingest.
3. **Read side** — after steps 3–4: Hangfire dashboard `/hangfire` → recurring
   jobs shows `fingerprint-ingest`; trigger it; it completes without errors;
   `fingerprints` / `fingerprint_occurrences` rows appear in Postgres and
   `ingest_cursor.last_polled_to` advances.
4. **GitHub actor** — after step 5: a fingerprint crossing the filing threshold
   (or a manual `POST /api/fingerprints/{id}/file-issue`) creates a labeled,
   assigned GitHub issue.
5. Confirm `ApplicationInsightsAgent_EXTENSION_VERSION` is absent (step 2.2).

## 7. Rollback / disable

- **Stop ingest:** blank or delete `FingerprintIngestSettings__WorkspaceId` and
  restart — the `RemoveIfExists` branch deregisters the persisted recurring job.
  Note: already-enqueued job *instances* (e.g. mid-retry after a failure) are not
  cancelled by this; delete them from the Hangfire dashboard if they matter.
- **Stop telemetry:** blank or delete `APPLICATIONINSIGHTS_CONNECTION_STRING`
  and restart — Serilog Console/File logging continues unaffected.
- **Stop issue filing only:** blank the `GitHubSettings--Token` Key Vault secret.
