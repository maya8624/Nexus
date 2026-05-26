namespace Nexus.Application.Dtos
{
    public sealed class EnquiryResponse
    {
        public Guid Id { get; init; }
        public Guid PropertyId { get; init; }
        public Guid? ListingId { get; init; }
        public Guid AgentId { get; init; }
        public string Body { get; init; } = string.Empty;
        public string? DraftReply { get; init; }
        public string? SentReply { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; init; }
        public DateTimeOffset? RepliedAtUtc { get; init; }
    }
}
