using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class DocumentSuggestionConfiguration : IEntityTypeConfiguration<DocumentSuggestion>
    {
        public void Configure(EntityTypeBuilder<DocumentSuggestion> builder)
        {
            builder.ToTable("document_suggestions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.DocId)
                .IsRequired();

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Suggestions)
                .HasColumnType("text[]")
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.ModelUsed)
                .HasMaxLength(100);

            builder.HasOne(x => x.User)
                .WithMany(x => x.DocumentSuggestions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.DocId);
            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
