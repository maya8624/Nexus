using Nexus.Application.Common;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces
{
    public interface IFingerprintAiService
    {
        Task<Result<FingerprintClassificationResult>> ClassifyAsync(Fingerprint fingerprint, CancellationToken ct);
        Task<Result<FingerprintIssueContent>> SummarizeAsync(Fingerprint fingerprint, IList<FingerprintOccurrence> recentOccurrences, CancellationToken ct);
    }
}
