namespace Nexus.Functions.Settings
{
    public class BlobIngestionSettings
    {
        public required string IngestionContainerName { get; init; }
        public required string InvoiceContainerName { get; init; }
        public required string NexusApiUrl { get; init; }
        public required string NexusApiKey { get; init; }
    }
}
