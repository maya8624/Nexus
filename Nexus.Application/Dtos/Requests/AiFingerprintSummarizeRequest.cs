namespace Nexus.Application.Dtos.Requests
{
    public class AiFingerprintSummarizeRequest
    {
        public required AiFingerprintSummarizeFingerprint fingerprint { get; init; }
        public required List<AiFingerprintSummarizeOccurrence> occurrences { get; init; }
    }

    public class AiFingerprintSummarizeFingerprint
    {
        public required string id { get; init; }
        public required string level { get; init; }
        public string? category { get; init; }
        public string? exception_type { get; init; }
        public required string message_template { get; init; }
        public string? operation { get; init; }
        public string? service_name { get; init; }
        public int total_count { get; init; }
        public DateTimeOffset first_seen { get; init; }
        public DateTimeOffset last_seen { get; init; }
    }

    public class AiFingerprintSummarizeOccurrence
    {
        public DateTimeOffset occurred_at { get; init; }
        public int occurrence_count { get; init; }
        public string? rendered_message { get; init; }
    }
}
