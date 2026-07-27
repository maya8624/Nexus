namespace Nexus.Application.Dtos.Responses
{
    public class AiFingerprintSummarizeResponse
    {
        public required string title { get; init; }
        public required string body { get; init; }
        public string? suggested_fix { get; init; }
    }
}
