namespace Nexus.Application.Dtos.Responses
{
    public sealed class FileUploadInitiatedResponse
    {
        public Guid FileUploadId { get; init; }
        public string SasUrl { get; init; } = default!;
        public string BlobName { get; init; } = default!;
        public DateTimeOffset SasExpiresAtUtc { get; init; }
    }
}
