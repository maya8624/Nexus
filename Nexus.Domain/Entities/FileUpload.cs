using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities
{
    public class FileUpload
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string FileName { get; set; } = default!;

        public string BlobName { get; set; } = default!;

        public string ContentType { get; set; } = default!;

        public long? FileSizeBytes { get; set; }

        public string ContainerName { get; set; } = default!;

        public UploadPurpose Purpose { get; set; }

        public UploadStatus Status { get; set; }

        public DateTimeOffset SasExpiresAtUtc { get; set; }

        public DateTimeOffset? CompletedAtUtc { get; set; }

        public string? ErrorMessage { get; set; }

        public IngestionStatus? IngestionStatus { get; set; }

        public string? IngestionError { get; set; }

        public DateTimeOffset? IngestedAtUtc { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public User User { get; set; } = default!;
    }
}
