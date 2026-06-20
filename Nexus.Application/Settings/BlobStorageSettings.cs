namespace Nexus.Application.Settings
{
    public class BlobStorageSettings
    {
        public required string ConnectionString { get; init; }
        public required string ContainerName { get; init; }
        public required string ExtractionContainerName { get; init; }
        public required string IngestionContainerName { get; init; }
        public required string InvoiceContainerName { get; init; }
        public int SasExpiryMinutes { get; init; } = 15;
    }
}
