namespace Nexus.Application.Dtos.Responses
{
    public class DocumentSuggestionResponse
    {
        public Guid Id { get; set; }
        public Guid DocId { get; set; }
        public Guid UserId { get; set; }
        public List<string> Suggestions { get; set; } = [];
        public string? ModelUsed { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
