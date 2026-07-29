namespace Nexus.Application.Dtos.Responses
{
    public sealed class FingerprintStatsResponse
    {
        public int OpenErrors { get; init; }
        public int OpenWarnings { get; init; }
        public int IssuesAssignedToday { get; init; }
        public int AgentPrsAwaitingReview { get; init; }
    }
}
