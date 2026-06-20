using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("invoices");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Filename).HasMaxLength(255);
            builder.Property(x => x.VendorName).HasMaxLength(500);
            builder.Property(x => x.VendorAddress).HasMaxLength(1000);
            builder.Property(x => x.CustomerName).HasMaxLength(500);
            builder.Property(x => x.InvoiceNumber).HasMaxLength(100);
            builder.Property(x => x.Currency).HasMaxLength(10);

            builder.Property(x => x.Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.Tax).HasPrecision(18, 2);
            builder.Property(x => x.Total).HasPrecision(18, 2);

            builder.OwnsMany(x => x.LineItems, li =>
            {
                li.ToJson();
                li.Property(x => x.Quantity).HasPrecision(18, 4);
                li.Property(x => x.UnitPrice).HasPrecision(18, 2);
                li.Property(x => x.Amount).HasPrecision(18, 2);
            });

            builder.HasOne(x => x.User)
                .WithMany(u => u.Invoices)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
