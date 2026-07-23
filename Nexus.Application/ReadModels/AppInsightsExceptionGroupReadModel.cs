namespace Nexus.Application.ReadModels
{
    public sealed class AppInsightsExceptionGroupReadModel
    {
        public string ProblemId { get; init; } = default!;
        public string ExceptionType { get; init; } = default!;
        public string? Operation { get; init; }
        public string? ServiceName { get; init; }
        public string SampleMessage { get; init; } = default!;
        public int Count { get; init; }
        public DateTimeOffset LastSeen { get; init; }
    }
}
