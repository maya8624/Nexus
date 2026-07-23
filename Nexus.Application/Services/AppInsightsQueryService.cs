using Azure.Monitor.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Interfaces;
using Nexus.Application.ReadModels;
using Nexus.Application.Settings;

namespace Nexus.Application.Services
{
    public class AppInsightsQueryService : IAppInsightsQueryService
    {
        // Groups by App Insights' own problemId so identical exceptions collapse into one row per window,
        // instead of one row per raw exception event.
        private const string ExceptionGroupsQuery = @"
exceptions
| summarize Count = count(), LastSeen = max(timestamp), SampleMessage = any(outerMessage) by ProblemId = problemId, ExceptionType = type, Operation = operation_Name, ServiceName = cloud_RoleName
| project ProblemId, ExceptionType, Operation, ServiceName, SampleMessage, Count, LastSeen";

        // severityLevel == 2 is Warning; traces have no problemId equivalent, so grouping falls back to raw message text.
        private const string TraceWarningsQuery = @"
traces
| where severityLevel == 2
| summarize Count = count(), LastSeen = max(timestamp) by RawMessage = message, Operation = operation_Name, ServiceName = cloud_RoleName
| project RawMessage, Operation, ServiceName, Count, LastSeen";

        private readonly LogsQueryClient _logsQueryClient;
        private readonly FingerprintIngestSettings _settings;
        private readonly ILogger<AppInsightsQueryService> _logger;

        public AppInsightsQueryService(
            LogsQueryClient logsQueryClient,
            IOptions<FingerprintIngestSettings> settings,
            ILogger<AppInsightsQueryService> logger)
        {
            _logsQueryClient = logsQueryClient;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Queries App Insights for exceptions in the given window, grouped by problem ID, exception type, operation, and service.
        /// </summary>
        /// <param name="from">Start of the query time range.</param>
        /// <param name="to">End of the query time range.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>One row per distinct exception group, with an occurrence count and last-seen timestamp.</returns>
        public async Task<IList<AppInsightsExceptionGroupReadModel>> QueryExceptionGroupsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        {
            var response = await _logsQueryClient.QueryWorkspaceAsync(
                _settings.WorkspaceId,
                ExceptionGroupsQuery,
                new QueryTimeRange(from, to),
                cancellationToken: ct);

            var result = response.Value.Table.Rows.Select(row => new AppInsightsExceptionGroupReadModel
            {
                ProblemId = row.GetString("ProblemId") ?? string.Empty,
                ExceptionType = row.GetString("ExceptionType") ?? string.Empty,
                Operation = row.GetString("Operation"),
                ServiceName = row.GetString("ServiceName"),
                SampleMessage = row.GetString("SampleMessage") ?? string.Empty,
                Count = row.GetInt32("Count") ?? 0,
                LastSeen = row.GetDateTimeOffset("LastSeen") ?? DateTimeOffset.MinValue
            }).ToList();

            _logger.LogDebug("Queried {Count} exception groups from App Insights for window {From:o}–{To:o}", result.Count, from, to);

            return result;
        }

        /// <summary>
        /// Queries App Insights for Warning-severity traces in the given window, grouped by raw message text, operation, and service.
        /// </summary>
        /// <param name="from">Start of the query time range.</param>
        /// <param name="to">End of the query time range.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>One row per distinct raw message group, with an occurrence count and last-seen timestamp.</returns>
        public async Task<IList<AppInsightsTraceWarningGroupReadModel>> QueryTraceWarningGroupsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        {
            var response = await _logsQueryClient.QueryWorkspaceAsync(
                _settings.WorkspaceId,
                TraceWarningsQuery,
                new QueryTimeRange(from, to),
                cancellationToken: ct);

            var result = response.Value.Table.Rows.Select(row => new AppInsightsTraceWarningGroupReadModel
            {
                RawMessage = row.GetString("RawMessage") ?? string.Empty,
                Operation = row.GetString("Operation"),
                ServiceName = row.GetString("ServiceName"),
                Count = row.GetInt32("Count") ?? 0,
                LastSeen = row.GetDateTimeOffset("LastSeen") ?? DateTimeOffset.MinValue
            }).ToList();

            _logger.LogDebug("Queried {Count} trace warning groups from App Insights for window {From:o}–{To:o}", result.Count, from, to);

            return result;
        }
    }
}
