using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Interfaces;
using Nexus.Application.ReadModels;
using Nexus.Application.Settings;

namespace Nexus.Application.Services
{
    public class AppInsightsQueryService : IAppInsightsQueryService
    {
        // Workspace-scope schema (AppExceptions/AppTraces, PascalCase columns) — these queries run via
        // QueryWorkspaceAsync against the Log Analytics workspace, not the App Insights resource, so the
        // classic names (exceptions/traces, problemId, cloud_RoleName) don't resolve here.
        //
        // Groups by App Insights' own ProblemId so identical exceptions collapse into one row per window,
        // instead of one row per raw exception event. Deliberately unfiltered by severity: an exception is
        // worth fingerprinting whatever level it was logged at. SeverityLevel is a grouping key rather
        // than a filter because the same ProblemId logged at two levels is two distinct fingerprints —
        // LogWarning(ex, …) lands here at severity 2 just as LogError(ex, …) lands at 3.
        private const string ExceptionGroupsQuery = @"
            AppExceptions
            | summarize Count = count(), LastSeen = max(TimeGenerated), SampleMessage = any(OuterMessage) by ProblemId, ExceptionType, Severity = SeverityLevel, Operation = OperationName, ServiceName = AppRoleName
            | project ProblemId, ExceptionType, Severity, Operation, ServiceName, SampleMessage, Count, LastSeen";

        // Traces are log records with no exception object attached, so they have no ProblemId and grouping
        // falls back to raw message text. Severity 2 is Warning, 3 Error, 4 Critical — all three are
        // ingested, because a LogError with nothing to throw reaches neither this table's old == 2 filter
        // nor AppExceptions, and would be invisible entirely. {0} is the category filter (see
        // BuildCategoryFilter); CategoryName lives inside the dynamic Properties column.
        private const string TraceGroupsQueryTemplate = @"
            AppTraces
            | where SeverityLevel >= 2
            | extend Category = tostring(parse_json(Properties).CategoryName){0}
            | summarize Count = count(), LastSeen = max(TimeGenerated) by RawMessage = Message, Severity = SeverityLevel, Operation = OperationName, ServiceName = AppRoleName
            | project RawMessage, Severity, Operation, ServiceName, Count, LastSeen";

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
            var timeRange = new QueryTimeRange(from, to);
            var response = await _logsQueryClient.QueryWorkspaceAsync(
                _settings.WorkspaceId,
                ExceptionGroupsQuery,
                timeRange,
                cancellationToken: ct);

            var result = response.Value.Table.Rows.Select(row => new AppInsightsExceptionGroupReadModel
            {
                ProblemId = row.GetString("ProblemId") ?? string.Empty,
                ExceptionType = row.GetString("ExceptionType") ?? string.Empty,
                Operation = row.GetString("Operation"),
                ServiceName = row.GetString("ServiceName"),
                SampleMessage = row.GetString("SampleMessage") ?? string.Empty,
                Severity = ReadSeverity(row),
                Count = (int)(row.GetInt64("Count") ?? 0),
                LastSeen = row.GetDateTimeOffset("LastSeen") ?? DateTimeOffset.MinValue
            }).ToList();

            _logger.LogDebug("Queried {Count} exception groups from App Insights for window {From:o}–{To:o}", result.Count, from, to);

            return result;
        }

        /// <summary>
        /// Queries App Insights for Warning-and-above traces in the given window, grouped by raw message text, severity, operation, and service.
        /// </summary>
        /// <param name="from">Start of the query time range.</param>
        /// <param name="to">End of the query time range.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>One row per distinct raw message and severity group, with an occurrence count and last-seen timestamp.</returns>
        public async Task<IList<AppInsightsTraceGroupReadModel>> QueryTraceGroupsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        {
            var query = string.Format(TraceGroupsQueryTemplate, BuildCategoryFilter(_settings.TraceCategoryPrefixes));
            var timeRange = new QueryTimeRange(from, to);
            var response = await _logsQueryClient.QueryWorkspaceAsync(
                _settings.WorkspaceId,
                query,
                timeRange,
                cancellationToken: ct);

            var result = response.Value.Table.Rows.Select(row => new AppInsightsTraceGroupReadModel
            {
                RawMessage = row.GetString("RawMessage") ?? string.Empty,
                Severity = ReadSeverity(row),
                Operation = row.GetString("Operation"),
                ServiceName = row.GetString("ServiceName"),
                Count = (int)(row.GetInt64("Count") ?? 0),
                LastSeen = row.GetDateTimeOffset("LastSeen") ?? DateTimeOffset.MinValue
            }).ToList();

            _logger.LogDebug("Queried {Count} trace groups from App Insights for window {From:o}–{To:o}", result.Count, from, to);

            return result;
        }

        /// <summary>
        /// Builds the KQL clause restricting traces to the configured logger category prefixes.
        /// </summary>
        /// <param name="prefixes">Category prefixes from configuration; empty or null disables the filter.</param>
        /// <returns>A leading-newline KQL <c>where</c> clause, or an empty string to ingest every category.</returns>
        private static string BuildCategoryFilter(string[]? prefixes)
        {
            var valid = (prefixes ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            if (valid.Length == 0)
                return string.Empty;

            // KQL has no vectorized startswith-any, so the prefixes become an OR chain. They come from
            // configuration rather than user input, but the quotes are escaped anyway — a stray quote in
            // a setting would otherwise produce a syntax error that only surfaces at ingest time.
            var clauses = valid.Select(p => $@"Category startswith ""{p.Replace(@"""", @"\""")}""");

            return "\n| where " + string.Join(" or ", clauses);
        }

        /// <summary>
        /// Reads the App Insights severity from a result row, defaulting to Warning when absent.
        /// </summary>
        /// <param name="row">A result row projecting a <c>Severity</c> column.</param>
        /// <returns>2 (Warning), 3 (Error), or 4 (Critical).</returns>
        private static int ReadSeverity(LogsTableRow row) => (int)(row.GetInt64("Severity") ?? 2);
    }
}
