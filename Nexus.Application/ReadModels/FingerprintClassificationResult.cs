using Nexus.Domain.Enums;

namespace Nexus.Application.ReadModels
{
    public sealed class FingerprintClassificationResult
    {
        public required FingerprintCategory Category { get; init; }
        public required double Confidence { get; init; }
        public required string Reason { get; init; }
        public required ClassificationSource Source { get; init; }
    }
}
