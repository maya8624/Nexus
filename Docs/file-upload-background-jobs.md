# File Upload — Background Jobs & Azure Function (Deferred)

## Hangfire Jobs

### Job 1: `ExpireStaleUploadsJob`
- **Schedule:** every 5 minutes (`*/5 * * * *`)
- **Logic:** `Status = Pending AND SasExpiresAtUtc < UtcNow` → bulk update to `Status = Expired`
- **Terminal state** — no retry needed. Hangfire handles job-level failures with default exponential backoff (10 attempts).

### Job 2: `StaleIngestionSweepJob`
- **Schedule:** every 15 minutes
- **Logic:** `IngestionStatus = Processing AND UpdatedAtUtc < UtcNow - 30min`
  - If `IngestionRetryCount < 3` → reset to `IngestionStatus = Queued`, increment `IngestionRetryCount`
  - If `IngestionRetryCount >= 3` → `IngestionStatus = Failed`, `IngestionError = "Max retries exceeded"`
- Requires `IngestionRetryCount int` column on `file_uploads` table (add to entity + migration when implementing)

---

## Azure Function

### Recommended trigger: Queue trigger (Azure Storage Queue)
- BlobTrigger fires once on blob landing — cannot be re-triggered for retries
- Queue trigger allows Hangfire to re-enqueue failed records by resetting `IngestionStatus = Queued`

### Flow
1. `POST /api/files/{id}/confirm` → sets `IngestionStatus = Queued` + puts message `{ fileUploadId }` onto the Storage Queue
2. Azure Function picks up message → calls back `PATCH /api/internal/files/{id}/ingestion` with `{ status: "Processing" }`
3. Azure Function calls Python ingestion service (`rec_brain` — `POST /chat` with `X-API-Key` header, settings under `AiServiceSettings`)
4. On success → callback with `{ status: "Completed" }`
5. On failure → callback with `{ status: "Failed", error: "..." }`

### Callback endpoint (`PATCH /api/internal/files/{id}/ingestion`)
- Auth: API key (not JWT) — Azure Function is not a user
- Different from user-facing endpoints — consider a dedicated `InternalController` (already exists at `Nexus.API/Controllers/InternalController.cs`)
- Updates `IngestionStatus`, `IngestionError`, `IngestedAtUtc`

### Retry
- Hangfire's `StaleIngestionSweepJob` resets stuck `Processing` records to `Queued`
- This re-enqueues the Storage Queue message → Azure Function picks it up again
- Max 3 retries before permanent `Failed`
