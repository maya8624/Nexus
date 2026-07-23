namespace Nexus.Application.ReadModels
{
    public sealed class AppInsightsTraceWarningGroupReadModel
    {
        public string RawMessage { get; init; } = default!;
        public string? Operation { get; init; }
        public string? ServiceName { get; init; }
        public int Count { get; init; }
        public DateTimeOffset LastSeen { get; init; }
    }
}
