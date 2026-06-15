namespace Nexus.Application.Dtos.Requests
{
    public record TriggerIngestionRequest(string BlobName, string ContainerName);
}
