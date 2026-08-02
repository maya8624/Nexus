using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Constants;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services
{
    public class FingerprintIngestJob : IFingerprintIngestJob
    {
        private readonly IAppInsightsQueryService _appInsightsQueryService;
        private readonly IFingerprinterService _fingerprinterService;
        private readonly IGitHubIssueService _gitHubIssueService;
        private readonly IIngestCursorRepository _cursorRepository;
        private readonly IUnitOfWork _uow;
        private readonly FingerprintIngestSettings _settings;
        private readonly ILogger<FingerprintIngestJob> _logger;

        public FingerprintIngestJob(
            IAppInsightsQueryService appInsightsQueryService,
            IFingerprinterService fingerprinterService,
            IGitHubIssueService gitHubIssueService,
            IIngestCursorRepository cursorRepository,
            IUnitOfWork uow,
            IOptions<FingerprintIngestSettings> settings,
            ILogger<FingerprintIngestJob> logger)
        {
            _appInsightsQueryService = appInsightsQueryService;
            _fingerprinterService = fingerprinterService;
            _gitHubIssueService = gitHubIssueService;
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

            if (exceptionGroups.Count == 0 && traceGroups.Count == 0)
            {
                _logger.LogDebug("Fingerprint ingest: no new events in window {From:o}-{To:o}.", from, to);
                return;
            }

            var newCount = await ProcessExceptionGroupsAsync(exceptionGroups, from, ct);
            newCount += await ProcessTraceGroupsAsync(traceGroups, from, ct);

            await _uow.SaveChanges();

            // Cursor is advanced and saved only after the fingerprint/occurrence batch above has
            // already committed, so a crash between the two SaveChanges calls re-polls the same
            // window next run instead of silently skipping unprocessed data.
            await AdvanceCursorAsync(cursor, to, ct);
            await _uow.SaveChanges();

            _logger.LogInformation(
                "Fingerprint ingest processed {ExceptionCount} exception groups and {TraceCount} trace groups ({NewCount} new) for window {From:o}-{To:o}.",
                exceptionGroups.Count, traceGroups.Count, newCount, from, to);
        }

        private async Task<int> ProcessExceptionGroupsAsync(
            IList<AppInsightsExceptionGroupReadModel> exceptionGroups, DateTimeOffset windowFrom, CancellationToken ct)
        {
            var newCount = 0;
            foreach (var row in exceptionGroups)
            {
                var (fingerprint, isNew, windowCount) = await _fingerprinterService.ProcessExceptionGroupAsync(row, windowFrom, ct);
                await _gitHubIssueService.ProcessFingerprintAsync(fingerprint, windowCount, isNew, ct);
                if (isNew)
                    newCount++;
            }

            return newCount;
        }

        private async Task<int> ProcessTraceGroupsAsync(
            IList<AppInsightsTraceGroupReadModel> traceGroups, DateTimeOffset windowFrom, CancellationToken ct)
        {
            var newCount = 0;
            foreach (var row in traceGroups)
            {
                var (fingerprint, isNew, windowCount) = await _fingerprinterService.ProcessTraceGroupAsync(row, windowFrom, ct);
                await _gitHubIssueService.ProcessFingerprintAsync(fingerprint, windowCount, isNew, ct);
                if (isNew)
                    newCount++;
            }

            return newCount;
        }

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
