namespace Nexus.Application.Dtos.Responses
{
    public class AiFingerprintClassifyResponse
    {
        public required string category { get; init; }
        public required double confidence { get; init; }
        public required string reason { get; init; }
    }
}
