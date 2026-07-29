namespace Nexus.Application.ReadModels
{
    public sealed class FingerprintStatsReadModel
    {
        public int OpenErrors { get; init; }
        public int OpenWarnings { get; init; }
        public int IssuesFiledToday { get; init; }
        public int PrsAwaitingReview { get; init; }
    }
}
