using Nexus.Application.Common;
using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Business
{
    public interface IGitHubIssueService
    {
        Task ProcessFingerprintAsync(Fingerprint fingerprint, int windowOccurrenceCount, bool isNewRegression, CancellationToken ct);
        Task<Result<Fingerprint>> ForceFileIssueAsync(Fingerprint fingerprint, CancellationToken ct);
        Task<Result<Fingerprint>> AddAutoFixCandidateLabelAsync(Fingerprint fingerprint, CancellationToken ct);
    }
}
