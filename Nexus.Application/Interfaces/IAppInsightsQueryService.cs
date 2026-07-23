using Nexus.Application.ReadModels;

namespace Nexus.Application.Interfaces
{
    public interface IAppInsightsQueryService
    {
        Task<IList<AppInsightsExceptionGroupReadModel>> QueryExceptionGroupsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
        Task<IList<AppInsightsTraceWarningGroupReadModel>> QueryTraceWarningGroupsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    }
}
