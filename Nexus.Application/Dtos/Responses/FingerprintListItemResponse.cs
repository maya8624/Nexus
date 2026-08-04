using Nexus.Domain.Enums;

namespace Nexus.Application.Dtos.Responses
{
    public class FingerprintListItemResponse
    {
        public required string Id { get; init; }
        public FingerprintLevel Level { get; init; }
        public FingerprintCategory? Category { get; init; }
        public string? ServiceName { get; init; }
        public string? Operation { get; init; }
        public int TotalCount { get; init; }
        public DateTimeOffset FirstSeenUtc { get; init; }
        public DateTimeOffset LastSeenUtc { get; init; }
        public GithubIssueStatus GithubStatus { get; init; }
        public int? GithubIssueNumber { get; init; }

        /// <summary>
        /// Derived from the configured owner/repo and <see cref="GithubIssueNumber"/>; null when the
        /// fingerprint has no issue or the server has no GitHub repository configured.
        /// </summary>
        public string? GithubIssueUrl { get; init; }

        public bool AutoFixEligible { get; init; }
        public IList<FingerprintSparklineBucketResponse> Sparkline { get; init; } = [];
    }

    public sealed class FingerprintSparklineBucketResponse
    {
        public DateTimeOffset BucketStart { get; init; }
        public int Count { get; init; }
    }
}
