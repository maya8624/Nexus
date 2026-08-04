using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Constants;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Org.BouncyCastle.Math.EC.Rfc7748;

namespace Nexus.Application.Services
{
    public class FingerprintIngestJob : IFingerprintIngestJob
    {
        private readonly IAppInsightsQueryService _appInsightsQueryService;
        private readonly IFingerprinterService _fingerprinterService;
        private readonly IGitHubIssueService _gitHubIssueService;
        private readonly IFingerprintRepository _fingerprintRepository;
        private readonly IIngestCursorRepository _cursorRepository;
        private readonly IUnitOfWork _uow;
        private readonly FingerprintIngestSettings _settings;
        private readonly ILogger<FingerprintIngestJob> _logger;

        public FingerprintIngestJob(
            IAppInsightsQueryService appInsightsQueryService,
            IFingerprinterService fingerprinterService,
            IGitHubIssueService gitHubIssueService,
            IFingerprintRepository fingerprintRepository,
            IIngestCursorRepository cursorRepository,
            IUnitOfWork uow,
            IOptions<FingerprintIngestSettings> settings,
            ILogger<FingerprintIngestJob> logger)
        {
            _appInsightsQueryService = appInsightsQueryService;
            _fingerprinterService = fingerprinterService;
            _gitHubIssueService = gitHubIssueService;
            _fingerprintRepository = fingerprintRepository;
            _cursorRepository = cursorRepository;
            _uow = uow;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var ct = CancellationToken.None;
            var cursor = await _cursorRepository.GetAsync(FingerprintConstants.IngestCursorSource, ct);
            var to = DateTimeOffset.UtcNow.AddMinutes(-_settings.IngestionSafetyLagMinutes);
            var from = cursor?.LastPolledTo ?? DateTimeOffset.UtcNow.AddMinutes(-_settings.InitialLookbackMinutes);

            if (from >= to)
            {
                _logger.LogDebug("Fingerprint ingest: window start {From:o} is not before window end {To:o}, skipping.", from, to);
                return;
            }

            var exceptionGroups = await _appInsightsQueryService.QueryExceptionGroupsAsync(from, to, ct);
            var traceGroups = await _appInsightsQueryService.QueryTraceGroupsAsync(from, to, ct);

            var mergedExceptionGroups = MergeByHash(exceptionGroups);
            var mergedTraceGroups = MergeByHash(traceGroups);

            if (mergedExceptionGroups.Count == 0 && mergedTraceGroups.Count == 0)
            {
                _logger.LogDebug("Fingerprint ingest: no new events in window {From:o}-{To:o}.", from, to);

                // A quiet window is exactly when a backlog of unfiled fingerprints should drain, so the
                // retry pass still runs. Nothing was staged, so there is nothing to commit or exclude.
                await RetryMissedIssueFilingsAsync([], ct);
                return;
            }

            var pendingActions = new List<PendingGitHubAction>(mergedExceptionGroups.Count + mergedTraceGroups.Count);
            await StageExceptionGroupsAsync(pendingActions, mergedExceptionGroups, from, ct);
            await StageTraceGroupsAsync(pendingActions, mergedTraceGroups, from, ct);

            await AdvanceCursorAsync(cursor, to, ct);
            await _uow.SaveChanges();

            await ProcessPendingGitHubActionsAsync(pendingActions, ct);

            _logger.LogInformation(
                "Fingerprint ingest read {ExceptionCount} exception and {TraceCount} trace group(s) from App Insights, merged to {FingerprintCount} fingerprint(s) ({NewCount} new), for window {From:o}-{To:o}.",
                exceptionGroups.Count, traceGroups.Count, pendingActions.Count, pendingActions.Count(x => x.IsNew), from, to);
        }

        /// <summary>
        /// Collapses App Insights rows that resolve to the same fingerprint into one row per hash,
        /// summing their counts.
        /// </summary>
        /// <remarks>
        /// The queries group by keys finer than the fingerprint hash: exceptions also group by
        /// <c>Operation</c>/<c>ServiceName</c> while the hash uses only <c>severity|problemId</c>, and
        /// traces group by raw <c>Message</c> while <c>NormalizeMessage</c> runs afterward here — so
        /// <c>Blob "a.pdf" not found</c> and <c>Blob "b.pdf" not found</c> arrive as two rows and are one
        /// fingerprint. Merging up front means every later stage sees one row per fingerprint: the
        /// fingerprinter never stages a duplicate id, exactly one occurrence row is written per window
        /// instead of one per source row, and the count handed to the filing threshold and the spike
        /// baseline is the window's real total rather than one slice of it.
        /// </remarks>
        private static IList<AppInsightsExceptionGroupReadModel> MergeByHash(IList<AppInsightsExceptionGroupReadModel> rows)
        {
            if (rows.Count < 2)
                return rows;

            return rows
                .GroupBy(x => FingerprintHasher.ComputeExceptionHash(x.ProblemId, x.Severity))
                .Select(group =>
                {
                    var first = group.First();
                    return group.Count() == 1 ? first : new AppInsightsExceptionGroupReadModel
                    {
                        ProblemId = first.ProblemId,
                        ExceptionType = first.ExceptionType,
                        Severity = first.Severity,
                        Operation = first.Operation,
                        ServiceName = first.ServiceName,
                        SampleMessage = first.SampleMessage,
                        Count = group.Sum(x => x.Count),
                        LastSeen = group.Max(x => x.LastSeen)
                    };
                })
                .ToList();
        }

        /// <inheritdoc cref="MergeByHash(IList{AppInsightsExceptionGroupReadModel})"/>
        private static IList<AppInsightsTraceGroupReadModel> MergeByHash(IList<AppInsightsTraceGroupReadModel> rows)
        {
            if (rows.Count < 2)
                return rows;

            return rows
                .GroupBy(x => FingerprintHasher.ComputeTraceHash(FingerprintHasher.NormalizeMessage(x.RawMessage), x.Severity))
                .Select(group =>
                {
                    var first = group.First();
                    return group.Count() == 1 ? first : new AppInsightsTraceGroupReadModel
                    {
                        RawMessage = first.RawMessage,
                        Severity = first.Severity,
                        Operation = first.Operation,
                        ServiceName = first.ServiceName,
                        Count = group.Sum(x => x.Count),
                        LastSeen = group.Max(x => x.LastSeen)
                    };
                })
                .ToList();
        }

        private async Task StageExceptionGroupsAsync(
            List<PendingGitHubAction> pendingActions, IList<AppInsightsExceptionGroupReadModel> exceptionGroups, DateTimeOffset windowFrom, CancellationToken ct)
        {
            foreach (var row in exceptionGroups)
            {
                var (fingerprint, isNew, windowCount) = await _fingerprinterService.ProcessExceptionGroupAsync(row, windowFrom, ct);
                pendingActions.Add(new PendingGitHubAction(fingerprint, windowCount, isNew));
            }
        }

        private async Task StageTraceGroupsAsync(
            List<PendingGitHubAction> pendingActions, IList<AppInsightsTraceGroupReadModel> traceGroups, DateTimeOffset windowFrom, CancellationToken ct)
        {
            foreach (var row in traceGroups)
            {
                var (fingerprint, isNew, windowCount) = await _fingerprinterService.ProcessTraceGroupAsync(row, windowFrom, ct);
                pendingActions.Add(new PendingGitHubAction(fingerprint, windowCount, isNew));
            }
        }

        /// <summary>
        /// Runs the GitHub actor for every fingerprint in the committed batch.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Must run only after the batch commit. The actor performs an external, non-transactional side
        /// effect (creating or updating a GitHub issue) and then commits the resulting issue number
        /// through this same unit of work. Two things go wrong if the rows are still unsaved.
        /// </para>
        /// <para>
        /// A brand-new fingerprint is still in the <c>Added</c> state, so the actor's <c>Update</c> moves
        /// it to <c>Modified</c> and its commit emits an UPDATE against a row that was never inserted —
        /// taking the occurrence staged alongside it down with a foreign key violation.
        /// </para>
        /// <para>
        /// More importantly, the issue would be created before the fingerprint is durable. Any failure
        /// between the two leaves an issue with no fingerprint behind it, and because
        /// <c>ProcessFingerprintAsync</c> swallows its exceptions, the next poll files the same issue
        /// again. Committing first means the actor only ever updates rows that already exist.
        /// </para>
        /// </remarks>
        private async Task ProcessPendingGitHubActionsAsync(IReadOnlyList<PendingGitHubAction> pendingActions, CancellationToken ct)
        {
            foreach (var item in pendingActions)
                await _gitHubIssueService.ProcessFingerprintAsync(item.Fingerprint, item.WindowCount, item.IsNew, ct);

            await RetryMissedIssueFilingsAsync(pendingActions, ct);
        }

        /// <summary>
        /// Retries fingerprints from earlier windows that still have no GitHub issue.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The current batch only covers fingerprints App Insights returned for this window, and the
        /// cursor has already advanced past every earlier one. So a fingerprint whose filing failed —
        /// the poll died before the actor ran, GitHub errored, the token was blank — is never looked at
        /// again unless the same error happens to recur. This retry pass is what closes that gap.
        /// </para>
        /// <para>
        /// Passed as <c>isNewRegression: true</c> on purpose, which bypasses
        /// <see cref="FingerprintFilingPolicy.ShouldFileIssue"/>. Every one of these already earned a
        /// "file it" from the policy when it was created — <c>isNew</c> short-circuits the threshold —
        /// and only failed to reach GitHub. Re-applying the threshold now would suppress them a second
        /// time for a reason the policy never intended.
        /// </para>
        /// <para>
        /// Known gap: <c>None</c> means the database has no issue number, which is not quite the same as
        /// "no issue exists". If <c>CreateIssueAsync</c> reached GitHub but its commit failed, this files
        /// a duplicate. That needs a database failure inside a one-call window, and a duplicate issue is
        /// visible and easy to close, so it is accepted rather than guarded.
        /// </para>
        /// </remarks>
        private async Task RetryMissedIssueFilingsAsync(IReadOnlyList<PendingGitHubAction> pendingActions, CancellationToken ct)
        {
            if (_settings.MissedIssueLookbackHours <= 0 || _settings.MaxMissedIssueRetriesPerRun <= 0)
                return;

            var createdSince = DateTimeOffset.UtcNow.AddHours(-_settings.MissedIssueLookbackHours);
            var unfiled = await _fingerprintRepository.GetUnfiledSinceAsync(createdSince, _settings.MaxMissedIssueRetriesPerRun, ct);
            if (unfiled.Count == 0)
                return;

            // Rows from this batch are already tracked and were just handed to the actor above. The query
            // above returns the same rows (a failed filing leaves them at None), so without this they
            // would be processed twice in one run.
            var handled = pendingActions.Select(x => x.Fingerprint.Id).ToHashSet();
            var missed = unfiled.Where(x => !handled.Contains(x.Id)).ToList();
            if (missed.Count == 0)
                return;

            _logger.LogInformation(
                "Fingerprint ingest retrying {Count} fingerprint(s) created since {CreatedSince:o} that still have no GitHub issue.",
                missed.Count, createdSince);

            foreach (var fingerprint in missed)
                await _gitHubIssueService.ProcessFingerprintAsync(fingerprint, fingerprint.TotalCount, isNewRegression: true, ct);
        }

        /// <summary>
        /// A fingerprint staged in the current batch, held until the commit lands so the GitHub actor can
        /// run against durable rows.
        /// </summary>
        private sealed record PendingGitHubAction(Fingerprint Fingerprint, int WindowCount, bool IsNew);

        private async Task AdvanceCursorAsync(IngestCursor? cursor, DateTimeOffset to, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            if (cursor is null)
            {
                await _cursorRepository.Create(new IngestCursor
                {
                    Source = FingerprintConstants.IngestCursorSource,
                    LastPolledTo = to,
                    UpdatedAtUtc = now
                }, ct);
            }
            else
            {
                cursor.LastPolledTo = to;
                cursor.UpdatedAtUtc = now;
                _cursorRepository.Update(cursor);
            }
        }
    }
}
