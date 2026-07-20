using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities
{
    public class Fingerprint
    {
        public string Id { get; set; } = default!;

        public string Hash { get; set; } = default!;

        public FingerprintLevel Level { get; set; }

        public FingerprintCategory? Category { get; set; }

        public ClassificationSource? ClassifiedBy { get; set; }

        public string? ExceptionType { get; set; }

        public string MessageTemplate { get; set; } = default!;

        public string? Operation { get; set; }

        public string? ServiceName { get; set; }

        public int TotalCount { get; set; }

        public DateTimeOffset FirstSeenUtc { get; set; }

        public DateTimeOffset LastSeenUtc { get; set; }

        public bool AutoFixEligible { get; set; }

        public int? GithubIssueNumber { get; set; }

        public GithubIssueStatus GithubStatus { get; set; }

        public DateTimeOffset? GithubLastCommentedAtUtc { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public ICollection<FingerprintOccurrence> Occurrences { get; set; } = [];
    }
}
