using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class FingerprintOccurrenceConfiguration : IEntityTypeConfiguration<FingerprintOccurrence>
    {
        public void Configure(EntityTypeBuilder<FingerprintOccurrence> builder)
        {
            builder.ToTable("fingerprint_occurrences");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.FingerprintId).HasMaxLength(16).IsRequired();
            builder.Property(x => x.RenderedMessage).HasMaxLength(2000);

            builder.Property(x => x.OccurredAt).IsRequired();
            builder.Property(x => x.OccurrenceCount).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasIndex(x => new { x.FingerprintId, x.OccurredAt });
        }
    }
}
