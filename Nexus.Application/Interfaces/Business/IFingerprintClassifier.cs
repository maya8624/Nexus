using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Business
{
    public interface IFingerprintClassifier
    {
        Task ClassifyAsync(Fingerprint fingerprint, bool isNewFingerprint, int windowOccurrenceCount, string? problemId, CancellationToken ct);
    }
}
