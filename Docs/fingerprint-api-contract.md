# Fingerprint Triage API — Frontend Contract

REST API for the fingerprint log-triage dashboard (Phase 6 of
`fingerprint-implementation-plan.md`). Everything documented here is live on the
.NET API. Intended audience: the React dashboard build.

**Auth:** every endpoint requires the standard JWT Bearer token (same as the rest
of the API). No anonymous access.
**JSON:** camelCase properties; all enums serialize as **strings** (e.g.
`"Error"`, not `1`).
**Errors:** non-2xx responses return the shared shape
`{ "code": 404, "name": "FingerprintNotFound", "message": "..." }`.

## Enums

| Enum | Values |
|---|---|
| `level` | `Error`, `Warning` |
| `category` | `DependencyFailure`, `NewRegression`, `RecurringKnown`, `ConfigAuth`, `DataQuality`, `Performance` (nullable — unclassified yet) |
| `githubStatus` | `None`, `Open`, `Closed`, `Pr`, `Merged` |
| `classifiedBy` | `Rule`, `Llm` (nullable) |

## Endpoints

### `GET /api/fingerprints` — list

Optional query params: `?status=Open&level=Error` (both filter exact-match; omit
for all). Returns an array sorted by `lastSeenUtc` descending:

```json
[
  {
    "id": "fp_a1b2c3d4",
    "level": "Error",
    "category": "DependencyFailure",
    "serviceName": "nexus-api-dev",
    "operation": "POST /api/internal/invoices/extract",
    "totalCount": 47,
    "firstSeenUtc": "2026-07-15T09:00:00+00:00",
    "lastSeenUtc": "2026-07-27T06:45:00+00:00",
    "githubStatus": "Open",
    "githubIssueNumber": 123,
    "autoFixEligible": true,
    "sparkline": [
      { "bucketStart": "2026-07-27T06:00:00+00:00", "count": 12 },
      { "bucketStart": "2026-07-27T05:00:00+00:00", "count": 3 }
    ]
  }
]
```

**Sparkline notes:** up to 7 hourly buckets, **newest first** — reverse the array
for a left-to-right time axis. Hours with zero occurrences have no bucket at all,
so pad gaps client-side if you want an even 7-slot chart.

### `GET /api/fingerprints/{id}` — detail

`404` if unknown id. Everything in the list item **plus**:

```json
{
  "...all list-item fields...": "...",
  "exceptionType": "System.Net.Http.HttpRequestException",
  "messageTemplate": "Connection refused to {n}.{n}.{n}.{n}:{n}",
  "classifiedBy": "Rule",
  "githubIssueFiledAtUtc": "2026-07-26T10:00:00+00:00",
  "githubLastCommentedAtUtc": "2026-07-27T06:00:00+00:00",
  "recentOccurrences": [
    { "occurredAt": "2026-07-27T06:45:00+00:00", "occurrenceCount": 12, "renderedMessage": "Connection refused to 10.0.0.4:5432" }
  ]
}
```

`recentOccurrences` is capped at 10, most recent first. `renderedMessage` is the
raw (un-normalized) sample — good for a "sample trace" panel.

### Actions — all `POST`, all return the updated **detail** payload on 200

| Endpoint | Success effect | 409 codes |
|---|---|---|
| `/api/fingerprints/{id}/file-issue` | Files (or reopens) a GitHub issue immediately, skipping the noise threshold | `IssueAlreadyOpen` |
| `/api/fingerprints/{id}/send-to-agent` | Adds the `auto-fix-candidate` label to the issue | `NotAutoFixEligible`, `NoGithubIssue` |
| `/api/fingerprints/{id}/resolve` | Closes the GitHub issue, sets `githubStatus` to `Closed` | `NoGithubIssue`, `PrInProgress`, `AlreadyResolved` |

All three also `404` (`FingerprintNotFound`) for an unknown id. The 409 `name`
field carries the code above — safe to switch on for toast messages.

**Suggested button-enable logic** (mirrors the server rules, avoids most 409s):

- **File issue**: `githubStatus === "None" || githubStatus === "Closed"`
- **Send to agent**: `autoFixEligible && githubIssueNumber !== null`
- **Resolve**: `githubStatus === "Open"`

### `GET /api/stats` — dashboard tiles

```json
{ "openErrors": 5, "openWarnings": 2, "issuesAssignedToday": 3, "agentPrsAwaitingReview": 0 }
```

- "Open" = any fingerprint whose `githubStatus` is not `Closed`/`Merged`
  (includes `None`).
- `issuesAssignedToday` = issues filed since UTC midnight.
- `agentPrsAwaitingReview` is **always 0 for now** — the agent-loop phase that
  sets `Pr` status doesn't exist yet. Render the tile, expect zero.

---

## Mock data for development

The backend ships a seeder that fills the API with full-state mock data:

```bash
dotnet run --project tools/FingerprintSeed
```

It inserts 12 fingerprints covering every `githubStatus` pill (`None`/`Open`/
`Closed`/`Pr`/`Merged`), all six categories plus two unclassified rows, varied
sparkline shapes (busy, spike, sparse, brand-new single occurrence), and
auto-fix-eligible rows. Re-run it any time — it wipes and reseeds only the
fingerprint tables, with timestamps relative to "now" so sparklines stay fresh.

Against this data expect: `GET /api/stats` →
`{ "openErrors": 6, "openWarnings": 4, "issuesAssignedToday": 2, "agentPrsAwaitingReview": 1 }`
and `GET /api/fingerprints` → 12 rows. The action endpoints will 404/409
normally, but note the GitHub issue numbers (#101–#107) are fake — "View on
GitHub" links won't resolve, and `file-issue`/`resolve` will fail at the real
GitHub call locally (no token configured), which is expected.

## Nullable / empty states to design for

`category`, `githubIssueNumber`, `classifiedBy`, `exceptionType`, `serviceName`,
`operation`, and the GitHub timestamps are all nullable. A brand-new fingerprint
can be `level: "Error"`, `category: null`, `githubStatus: "None"` with an empty
sparkline — the list row and detail page must handle those gracefully.
