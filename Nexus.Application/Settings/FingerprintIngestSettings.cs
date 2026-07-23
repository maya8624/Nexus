namespace Nexus.Application.Settings
{
    public class FingerprintIngestSettings
    {
        public required string WorkspaceId { get; init; }
        public int InitialLookbackMinutes { get; init; } = 60;
        public int IngestionSafetyLagMinutes { get; init; } = 5;
    }
}
