using Nexus.Application.Common;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class FingerprinterService : IFingerprinterService
    {
        private readonly IFingerprintRepository _fingerprintRepository;
        private readonly IFingerprintOccurrenceRepository _fingerprintOccurrenceRepository;
        private readonly IFingerprintClassifier _classifier;

        public FingerprinterService(
            IFingerprintRepository fingerprintRepository,
            IFingerprintOccurrenceRepository fingerprintOccurrenceRepository,
            IFingerprintClassifier classifier)
        {
            _fingerprintRepository = fingerprintRepository;
            _fingerprintOccurrenceRepository = fingerprintOccurrenceRepository;
            _classifier = classifier;
        }

        public async Task<(Fingerprint Fingerprint, bool IsNewFingerprint, int WindowCount)> ProcessExceptionGroupAsync(
            AppInsightsExceptionGroupReadModel row, DateTimeOffset windowFrom, CancellationToken ct)
        {
            var hash = FingerprintHasher.ComputeExceptionHash(row.ProblemId, row.Severity);
            var existing = await _fingerprintRepository.GetByHashAsync(hash, ct);
            var isNew = existing is null;
            var now = DateTimeOffset.UtcNow;

            if (existing is null)
            {
                existing = new Fingerprint
                {
                    Id = FingerprintHasher.GenerateFingerprintId(hash),
                    Hash = hash,
                    Level = MapLevel(row.Severity),
                    ExceptionType = Truncate(row.ExceptionType, 500),
                    MessageTemplate = Truncate(row.SampleMessage, 2000)!,
                    Operation = Truncate(row.Operation, 300),
                    ServiceName = Truncate(row.ServiceName, 200),
                    TotalCount = row.Count,
                    FirstSeenUtc = row.LastSeen,
                    LastSeenUtc = row.LastSeen,
                    GithubStatus = GithubIssueStatus.None,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                await _fingerprintRepository.Create(existing, ct);
            }
            else
            {
                UpdateFingerprint(existing, row.Count, row.LastSeen, now);
            }

            await _classifier.ClassifyAsync(existing, isNew, row.Count, row.ProblemId, ct);

            await AddOccurrenceAsync(existing.Id, windowFrom, row.Count, row.SampleMessage, now, ct);

            return (existing, isNew, row.Count);
        }

        public async Task<(Fingerprint Fingerprint, bool IsNewFingerprint, int WindowCount)> ProcessTraceGroupAsync(
            AppInsightsTraceGroupReadModel row, DateTimeOffset windowFrom, CancellationToken ct)
        {
            var normalized = FingerprintHasher.NormalizeMessage(row.RawMessage);
            var hash = FingerprintHasher.ComputeTraceHash(normalized, row.Severity);
            var existing = await _fingerprintRepository.GetByHashAsync(hash, ct);
            var isNew = existing is null;
            var now = DateTimeOffset.UtcNow;

            if (existing is null)
            {
                existing = new Fingerprint
                {
                    Id = FingerprintHasher.GenerateFingerprintId(hash),
                    Hash = hash,
                    Level = MapLevel(row.Severity),
                    ExceptionType = null,
                    MessageTemplate = Truncate(normalized, 2000)!,
                    Operation = Truncate(row.Operation, 300),
                    ServiceName = Truncate(row.ServiceName, 200),
                    TotalCount = row.Count,
                    FirstSeenUtc = row.LastSeen,
                    LastSeenUtc = row.LastSeen,
                    GithubStatus = GithubIssueStatus.None,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                await _fingerprintRepository.Create(existing, ct);
            }
            else
            {
                UpdateFingerprint(existing, row.Count, row.LastSeen, now);
            }

            await _classifier.ClassifyAsync(existing, isNew, row.Count, problemId: null, ct);

            await AddOccurrenceAsync(existing.Id, windowFrom, row.Count, row.RawMessage, now, ct);

            return (existing, isNew, row.Count);
        }

        // Severity is the only trustworthy source for Level: the source table indicates whether an
        // exception object was attached, not how bad the event was. LogWarning(ex, …) produces an
        // AppExceptions row at severity 2, and LogError("…") produces an AppTraces row at severity 3.
        // Critical (4) folds into Error — both mean "file it and look now", and splitting them would
        // ripple into /api/stats, the GitHub severity labels, and the dashboard's level filter for no
        // triage gain. The raw severity still reaches the hash, so that choice stays reversible.
        private static FingerprintLevel MapLevel(int severity)
            => severity >= 3 ? FingerprintLevel.Error : FingerprintLevel.Warning;

        private Fingerprint UpdateFingerprint(Fingerprint existing, int count, DateTimeOffset lastSeen, DateTimeOffset now)
        {
            existing.TotalCount += count;
            existing.LastSeenUtc = lastSeen;
            existing.UpdatedAtUtc = now;
            _fingerprintRepository.Update(existing);
            return existing;
        }

        private async Task AddOccurrenceAsync(
            string fingerprintId, DateTimeOffset windowFrom, int count, string? renderedMessage, DateTimeOffset now, CancellationToken ct)
        {
            await _fingerprintOccurrenceRepository.Create(new FingerprintOccurrence
            {
                FingerprintId = fingerprintId,
                OccurredAt = windowFrom,
                OccurrenceCount = count,
                RenderedMessage = Truncate(renderedMessage, 2000),
                CreatedAtUtc = now
            }, ct);
        }

        // Postgres varchar(n) columns (Phase 1 migration) enforce length at the DB layer;
        // truncating oversized App Insights text here avoids a poison-pill row that fails every retry.
        private static string? Truncate(string? value, int maxLength)
            => value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
