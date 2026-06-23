using Nexus.Domain.Enums;

namespace Nexus.Application.Dtos.Responses
{
    public class FileUploadResponse
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string FileName { get; init; } = default!;
        public string BlobName { get; init; } = default!;
        public string ContentType { get; init; } = default!;
        public long? FileSizeBytes { get; init; }
        public UploadPurpose Purpose { get; init; }
        public UploadStatus Status { get; init; }
        public IngestionStatus? IngestionStatus { get; init; }
        public string? IngestionError { get; init; }
        public DateTimeOffset? CompletedAtUtc { get; init; }
        public DateTimeOffset? IngestedAtUtc { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
    }
}
