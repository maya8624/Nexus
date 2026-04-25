using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class DepositConfiguration : IEntityTypeConfiguration<Deposit>
    {
        public void Configure(EntityTypeBuilder<Deposit> builder)
        {
            builder.ToTable("deposits");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();

            builder.Property(x => x.Currency)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.StripeSessionId).HasMaxLength(200).IsRequired();
            builder.Property(x => x.StripeSessionUrl).HasMaxLength(2048);
            builder.Property(x => x.StripePaymentIntentId).HasMaxLength(200);
            builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
            builder.Property(x => x.PaidAtUtc);
            builder.Property(x => x.RawResponse).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();

            builder.HasIndex(x => x.StripeSessionId).IsUnique();
            builder.HasIndex(x => x.IdempotencyKey).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.PropertyId);
            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => x.Status);

            builder.HasOne(x => x.User)
                .WithMany(u => u.Deposits)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Property)
                .WithMany(p => p.Deposits)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Listing)
                .WithMany(l => l.Deposits)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
