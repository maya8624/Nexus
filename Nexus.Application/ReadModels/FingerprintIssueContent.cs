namespace Nexus.Application.ReadModels
{
    public sealed class FingerprintIssueContent
    {
        public required string Title { get; init; }
        public required string Body { get; init; }
        public string? SuggestedFix { get; init; }
    }
}
