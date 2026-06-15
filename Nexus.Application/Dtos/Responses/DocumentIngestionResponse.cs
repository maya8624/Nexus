namespace Nexus.Application.Dtos.Responses
{
    public record DocumentIngestionResponse(
        string FileName,
        string? PropertyId,
        string? DocType,
        int ChunkCount,
        string Message
    );
}
