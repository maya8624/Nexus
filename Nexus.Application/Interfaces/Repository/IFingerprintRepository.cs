using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IFingerprintRepository : IRepositoryBase<Fingerprint>
    {
        Task<Fingerprint?> GetByIdAsync(string id, CancellationToken ct);
        Task<Fingerprint?> GetByHashAsync(string hash, CancellationToken ct);
        Task<IList<Fingerprint>> GetOpenAsync(FingerprintLevel? level, CancellationToken ct);
        Task<IList<Fingerprint>> GetListAsync(GithubIssueStatus? status, FingerprintLevel? level, CancellationToken ct);
        Task<IList<Fingerprint>> GetUnfiledSinceAsync(DateTimeOffset createdSince, int take, CancellationToken ct);
        Task<FingerprintStatsReadModel> GetStatsAsync(DateTimeOffset todayStartUtc, CancellationToken ct);
        Task<IList<FingerprintSparklineBucketReadModel>> GetSparklineBucketsAsync(string fingerprintId, int bucketCount, CancellationToken ct);
        Task<FingerprintHourlyBaselineReadModel?> GetHourlyBaselineAsync(string fingerprintId, CancellationToken ct);
    }
}
