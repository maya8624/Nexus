using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("tenants");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Phone).HasMaxLength(20);

            builder.Property(x => x.LeaseStartDate).IsRequired();
            builder.Property(x => x.LeaseEndDate).IsRequired();

            builder.Property(x => x.WeeklyRent).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.BondAmount).HasPrecision(18, 2).IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Property)
                .WithMany()
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Nullable pointer to the current active lease
            builder.HasOne(x => x.Lease)
                .WithMany()
                .HasForeignKey(x => x.LeaseId)
                .OnDelete(DeleteBehavior.SetNull);

            // Full lease history via Lease.TenantId
            builder.HasMany(x => x.Leases)
                .WithOne(x => x.Tenant)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.PropertyId);
            builder.HasIndex(x => x.LeaseId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Email);
        }
    }
}
