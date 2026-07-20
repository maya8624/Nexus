using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class FingerprintConfiguration : IEntityTypeConfiguration<Fingerprint>
    {
        public void Configure(EntityTypeBuilder<Fingerprint> builder)
        {
            builder.ToTable("fingerprints");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasMaxLength(16).ValueGeneratedNever();

            builder.Property(x => x.Hash).HasMaxLength(64).IsRequired();

            builder.Property(x => x.ExceptionType).HasMaxLength(500);
            builder.Property(x => x.MessageTemplate).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.Operation).HasMaxLength(300);
            builder.Property(x => x.ServiceName).HasMaxLength(200);

            builder.Property(x => x.Level)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(x => x.Category)
                .HasConversion<string>();

            builder.Property(x => x.ClassifiedBy)
                .HasConversion<string>();

            builder.Property(x => x.GithubStatus)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(x => x.TotalCount).IsRequired();
            builder.Property(x => x.FirstSeenUtc).IsRequired();
            builder.Property(x => x.LastSeenUtc).IsRequired();
            builder.Property(x => x.AutoFixEligible).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();

            builder.HasMany(x => x.Occurrences)
                .WithOne(x => x.Fingerprint)
                .HasForeignKey(x => x.FingerprintId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Hash).IsUnique();
            builder.HasIndex(x => new { x.GithubStatus, x.Level });
        }
    }
}
