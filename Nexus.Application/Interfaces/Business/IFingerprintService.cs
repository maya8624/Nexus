using Nexus.Application.Common;
using Nexus.Application.Dtos.Responses;
using Nexus.Domain.Enums;

namespace Nexus.Application.Interfaces.Business
{
    public interface IFingerprintService
    {
        Task<Result<IList<FingerprintListItemResponse>>> GetListAsync(GithubIssueStatus? status, FingerprintLevel? level, CancellationToken ct);
        Task<Result<FingerprintDetailResponse>> GetByIdAsync(string id, CancellationToken ct);
        Task<Result<FingerprintDetailResponse>> FileIssueAsync(string id, CancellationToken ct);
        Task<Result<FingerprintDetailResponse>> SendToAgentAsync(string id, CancellationToken ct);
        Task<Result<FingerprintDetailResponse>> ResolveAsync(string id, CancellationToken ct);
        Task<Result<FingerprintStatsResponse>> GetStatsAsync(CancellationToken ct);
    }
}
