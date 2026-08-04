using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class FingerprintRepository : RepositoryBase<Fingerprint>, IFingerprintRepository
    {
        private readonly AppDbContext _context;

        public FingerprintRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Fingerprint?> GetByIdAsync(string id, CancellationToken ct)
        {
            return await Find(id, ct);
        }

        public async Task<Fingerprint?> GetByHashAsync(string hash, CancellationToken ct)
        {
            return await _context.Fingerprints
                .Where(x => x.Hash == hash)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IList<Fingerprint>> GetOpenAsync(FingerprintLevel? level, CancellationToken ct)
        {
            return await _context.Fingerprints
                .Where(x => x.GithubStatus != GithubIssueStatus.Closed && x.GithubStatus != GithubIssueStatus.Merged)
                .Where(x => level == null || x.Level == level)
                .OrderByDescending(x => x.LastSeenUtc)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<IList<Fingerprint>> GetListAsync(GithubIssueStatus? status, FingerprintLevel? level, CancellationToken ct)
        {
            return await _context.Fingerprints
                .Where(x => status == null || x.GithubStatus == status)
                .Where(x => level == null || x.Level == level)
                .OrderByDescending(x => x.LastSeenUtc)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<IList<Fingerprint>> GetUnfiledSinceAsync(DateTimeOffset createdSince, int take, CancellationToken ct)
        {
            // Deliberately tracked, unlike the dashboard's GetListAsync: the ingest job's retry pass hands
            // these straight to the GitHub actor, which calls Update and SaveChanges on them. A detached
            // copy of a row already tracked by this context would throw on Update.
            //
            // Oldest first, so a backlog larger than the per-run cap drains in arrival order rather than
            // starving the earliest failures.
            return await _context.Fingerprints
                .Where(x => x.GithubStatus == GithubIssueStatus.None && x.CreatedAtUtc >= createdSince)
                .OrderBy(x => x.CreatedAtUtc)
                .Take(take)
                .ToListAsync(ct);
        }

        public async Task<FingerprintStatsReadModel> GetStatsAsync(DateTimeOffset todayStartUtc, CancellationToken ct)
        {
            var openErrors = await _context.Fingerprints
                .CountAsync(x => x.GithubStatus != GithubIssueStatus.Closed
                                 && x.GithubStatus != GithubIssueStatus.Merged
                                 && x.Level == FingerprintLevel.Error, ct);

            var openWarnings = await _context.Fingerprints
                .CountAsync(x => x.GithubStatus != GithubIssueStatus.Closed
                                 && x.GithubStatus != GithubIssueStatus.Merged
                                 && x.Level == FingerprintLevel.Warning, ct);

            var issuesFiledToday = await _context.Fingerprints
                .CountAsync(x => x.GithubIssueFiledAtUtc != null && x.GithubIssueFiledAtUtc >= todayStartUtc, ct);

            var prsAwaitingReview = await _context.Fingerprints
                .CountAsync(x => x.GithubStatus == GithubIssueStatus.Pr, ct);

            return new FingerprintStatsReadModel
            {
                OpenErrors = openErrors,
                OpenWarnings = openWarnings,
                IssuesFiledToday = issuesFiledToday,
                PrsAwaitingReview = prsAwaitingReview
            };
        }

        public async Task<IList<FingerprintSparklineBucketReadModel>> GetSparklineBucketsAsync(string fingerprintId, int bucketCount, CancellationToken ct)
        {
            var grouped = await _context.FingerprintOccurrences
                .Where(x => x.FingerprintId == fingerprintId)
                .GroupBy(x => new { x.OccurredAt.Year, x.OccurredAt.Month, x.OccurredAt.Day, x.OccurredAt.Hour })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, Count = g.Sum(x => x.OccurrenceCount) })
                .ToListAsync(ct);

            return grouped
                .Select(x => new FingerprintSparklineBucketReadModel
                {
                    BucketStart = new DateTimeOffset(x.Year, x.Month, x.Day, x.Hour, 0, 0, TimeSpan.Zero),
                    Count = x.Count
                })
                .OrderByDescending(x => x.BucketStart)
                .Take(bucketCount)
                .ToList();
        }

        public async Task<FingerprintHourlyBaselineReadModel?> GetHourlyBaselineAsync(string fingerprintId, CancellationToken ct)
        {
            var buckets = await _context.FingerprintOccurrences
                .Where(x => x.FingerprintId == fingerprintId)
                .GroupBy(x => new { x.OccurredAt.Year, x.OccurredAt.Month, x.OccurredAt.Day, x.OccurredAt.Hour })
                .Select(g => g.Sum(x => x.OccurrenceCount))
                .ToListAsync(ct);

            if (buckets.Count == 0)
            {
                return null;
            }

            return new FingerprintHourlyBaselineReadModel
            {
                AverageHourlyCount = buckets.Average(),
                SampleHours = buckets.Count
            };
        }
    }
}
