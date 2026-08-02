namespace Nexus.Application.ReadModels
{
    public sealed class AppInsightsExceptionGroupReadModel
    {
        public string ProblemId { get; init; } = default!;
        public string ExceptionType { get; init; } = default!;

        /// <summary>
        /// Raw App Insights severity (2 = Warning, 3 = Error, 4 = Critical). Exceptions are not
        /// always logged at Error — <c>LogWarning(ex, …)</c> produces an AppExceptions row at
        /// severity 2 — so this, not the source table, determines
        /// <see cref="Nexus.Domain.Enums.FingerprintLevel"/>.
        /// </summary>
        public int Severity { get; init; }

        public string? Operation { get; init; }
        public string? ServiceName { get; init; }
        public string SampleMessage { get; init; } = default!;
        public int Count { get; init; }
        public DateTimeOffset LastSeen { get; init; }
    }
}
