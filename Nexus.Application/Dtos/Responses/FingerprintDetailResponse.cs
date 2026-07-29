using Nexus.Domain.Enums;

namespace Nexus.Application.Dtos.Responses
{
    public sealed class FingerprintDetailResponse : FingerprintListItemResponse
    {
        public string? ExceptionType { get; init; }
        public required string MessageTemplate { get; init; }
        public ClassificationSource? ClassifiedBy { get; init; }
        public DateTimeOffset? GithubIssueFiledAtUtc { get; init; }
        public DateTimeOffset? GithubLastCommentedAtUtc { get; init; }
        public IList<FingerprintOccurrenceResponse> RecentOccurrences { get; init; } = [];
    }

    public sealed class FingerprintOccurrenceResponse
    {
        public DateTimeOffset OccurredAt { get; init; }
        public int OccurrenceCount { get; init; }
        public string? RenderedMessage { get; init; }
    }
}
