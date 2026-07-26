namespace Nexus.Application.Dtos.Requests
{
    public class AiFingerprintClassifyRequest
    {
        public string? exception_type { get; init; }
        public required string message_template { get; init; }
        public string? sample_trace { get; init; }
        public string? operation { get; init; }
    }
}
