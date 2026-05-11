using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();
            
            builder.Property(x => x.TokenHash)
                .HasMaxLength(64)
                .IsRequired();
            
            builder.HasIndex(x => x.TokenHash)
                .IsUnique();
            
            builder.Property(x => x.ExpiresAt)
                .IsRequired();
            
            builder.Property(x => x.IsRevoked)
                .IsRequired();
            
            builder.Property(x => x.CreatedAt)
                .IsRequired();
            
            builder.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
