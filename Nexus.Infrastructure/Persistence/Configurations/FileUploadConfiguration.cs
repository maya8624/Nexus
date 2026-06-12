using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class FileUploadConfiguration : IEntityTypeConfiguration<FileUpload>
    {
        public void Configure(EntityTypeBuilder<FileUpload> builder)
        {
            builder.ToTable("file_uploads");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            builder.Property(x => x.BlobName).HasMaxLength(500).IsRequired();
            builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            builder.Property(x => x.ContainerName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.FileSizeBytes);
            builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
            builder.Property(x => x.IngestionError).HasMaxLength(2000);

            builder.Property(x => x.Purpose)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.IngestionStatus)
                .HasConversion<int?>();

            builder.Property(x => x.SasExpiresAtUtc).IsRequired();
            builder.Property(x => x.CompletedAtUtc);
            builder.Property(x => x.IngestedAtUtc);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();

            builder.HasOne(x => x.User)
                .WithMany(u => u.FileUploads)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => new { x.Status, x.SasExpiresAtUtc });
            builder.HasIndex(x => x.IngestionStatus);
        }
    }
}
