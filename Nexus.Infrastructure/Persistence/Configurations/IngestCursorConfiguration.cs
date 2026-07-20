using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class IngestCursorConfiguration : IEntityTypeConfiguration<IngestCursor>
    {
        public void Configure(EntityTypeBuilder<IngestCursor> builder)
        {
            builder.ToTable("ingest_cursor");

            builder.HasKey(x => x.Source);
            builder.Property(x => x.Source).HasMaxLength(50).ValueGeneratedNever();

            builder.Property(x => x.LastPolledTo).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
        }
    }
}
