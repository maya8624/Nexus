namespace Nexus.Application.Dtos.Requests
{
    public class SaveDocumentSuggestionRequest
    {
        public Guid DocId { get; set; }
        public Guid UserId { get; set; }
        public List<string> Suggestions { get; set; } = [];
        public string? ModelUsed { get; set; }
    }
}
