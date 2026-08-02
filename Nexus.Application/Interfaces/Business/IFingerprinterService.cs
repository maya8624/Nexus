using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Business
{
    public interface IFingerprinterService
    {
        Task<(Fingerprint Fingerprint, bool IsNewFingerprint, int WindowCount)> ProcessExceptionGroupAsync(
            AppInsightsExceptionGroupReadModel row,
            DateTimeOffset windowFrom,
            CancellationToken ct);

        Task<(Fingerprint Fingerprint, bool IsNewFingerprint, int WindowCount)> ProcessTraceGroupAsync(
            AppInsightsTraceGroupReadModel row,
            DateTimeOffset windowFrom,
            CancellationToken ct);
    }
}
