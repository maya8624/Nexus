namespace Nexus.Application.Dtos.Responses
{
    // snake_case to match Python response schema exactly
    internal record AiIngestionResponse(
        bool success,
        string filename,
        string? property_id,
        string? doc_type,
        int chunk_count,
        string message
    );
}
