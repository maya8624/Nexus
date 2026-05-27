namespace Nexus.Application.Dtos.Responses
{
    public sealed class EnquiryDraftResponse
    {
        public string Draft { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public List<string> Sources { get; init; } = [];
    }

    public sealed class AiEnquiryDraftRequest
    {
        public required string id { get; init; }
        public required string body { get; init; }
        public string? tenant_id { get; init; }
        public string? property_id { get; init; }
        public string? intent { get; init; }
    }

    public sealed class AiEnquiryDraftResponse
    {
        public required string draft { get; init; }
        public List<string> sources { get; init; } = [];
    }
}
