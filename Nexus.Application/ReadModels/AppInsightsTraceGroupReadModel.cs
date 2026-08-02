namespace Nexus.Application.ReadModels
{
    public sealed class AppInsightsTraceGroupReadModel
    {
        public string RawMessage { get; init; } = default!;

        /// <summary>
        /// Raw App Insights severity (2 = Warning, 3 = Error, 4 = Critical). Set by the call site's
        /// log level, never inferred by App Insights, so it is the only reliable source for
        /// <see cref="Nexus.Domain.Enums.FingerprintLevel"/> — the source table only indicates
        /// whether an exception object was attached.
        /// </summary>
        public int Severity { get; init; }

        public string? Operation { get; init; }
        public string? ServiceName { get; init; }
        public int Count { get; init; }
        public DateTimeOffset LastSeen { get; init; }
    }
}
