namespace Nexus.Application.Settings
{
    public class BlobStorageSettings
    {
        public required string ConnectionString { get; init; }
        public required string ContainerName { get; init; }
        public int SasExpiryMinutes { get; init; } = 15;
    }
}
