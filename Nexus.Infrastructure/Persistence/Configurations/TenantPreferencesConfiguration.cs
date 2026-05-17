using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class TenantPreferencesConfiguration : IEntityTypeConfiguration<TenantPreferences>
    {
        public void Configure(EntityTypeBuilder<TenantPreferences> builder)
        {
            builder.ToTable("tenant_preferences");
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.UserId)
                .ValueGeneratedNever();

            builder.Property(x => x.Suburbs)
                .HasColumnType("text[]");

            builder.Property(x => x.MaxRent).IsRequired();
            builder.Property(x => x.MinBeds).IsRequired();
            builder.Property(x => x.MaxBeds).IsRequired();
            builder.Property(x => x.PetFriendly).IsRequired();
            builder.Property(x => x.AvailableWithinDays).IsRequired();

            builder.HasOne(x => x.User)
                .WithOne(x => x.TenantPreferences)
                .HasForeignKey<TenantPreferences>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
