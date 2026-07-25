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

        public FingerprinterService(
            IFingerprintRepository fingerprintRepository,
            IFingerprintOccurrenceRepository fingerprintOccurrenceRepository)
        {
            _fingerprintRepository = fingerprintRepository;
            _fingerprintOccurrenceRepository = fingerprintOccurrenceRepository;
        }

        public async Task<(Fingerprint Fingerprint, bool IsNewFingerprint, int WindowCount)> ProcessExceptionGroupAsync(
            AppInsightsExceptionGroupReadModel row, DateTimeOffset windowFrom, CancellationToken ct)
        {
            var hash = FingerprintHasher.ComputeExceptionHash(row.ProblemId);
            var existing = await _fingerprintRepository.GetByHashAsync(hash, ct);
            var isNew = existing is null;
            var now = DateTimeOffset.UtcNow;

            if (existing is null)
            {
                existing = new Fingerprint
                {
                    Id = FingerprintHasher.GenerateFingerprintId(hash),
                    Hash = hash,
                    Level = FingerprintLevel.Error,
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

            await AddOccurrenceAsync(existing.Id, windowFrom, row.Count, row.SampleMessage, now, ct);

            return (existing, isNew, row.Count);
        }

        public async Task<(Fingerprint Fingerprint, bool IsNewFingerprint, int WindowCount)> ProcessTraceWarningGroupAsync(
            AppInsightsTraceWarningGroupReadModel row, DateTimeOffset windowFrom, CancellationToken ct)
        {
            var normalized = FingerprintHasher.NormalizeMessage(row.RawMessage);
            var hash = FingerprintHasher.ComputeTraceHash(normalized);
            var existing = await _fingerprintRepository.GetByHashAsync(hash, ct);
            var isNew = existing is null;
            var now = DateTimeOffset.UtcNow;

            if (existing is null)
            {
                existing = new Fingerprint
                {
                    Id = FingerprintHasher.GenerateFingerprintId(hash),
                    Hash = hash,
                    Level = FingerprintLevel.Warning,
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

            await AddOccurrenceAsync(existing.Id, windowFrom, row.Count, row.RawMessage, now, ct);

            return (existing, isNew, row.Count);
        }

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
