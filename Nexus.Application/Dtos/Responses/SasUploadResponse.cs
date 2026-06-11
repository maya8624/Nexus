namespace Nexus.Application.Dtos.Responses
{
    public sealed class SasUploadResponse
    {
        public string SasUrl { get; init; } = default!;
        public string BlobName { get; init; } = default!;
    }
}
