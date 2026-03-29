using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class PropertyAddressConfiguration : IEntityTypeConfiguration<PropertyAddress>
    {
        public void Configure(EntityTypeBuilder<PropertyAddress> builder)
        {
            builder.ToTable("property_addresses");

            builder.HasKey(x => x.PropertyId);

            builder.Property(x => x.AddressLine1)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.AddressLine2)
                .HasMaxLength(200);

            builder.Property(x => x.Suburb)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.State)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Postcode)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Country)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Latitude)
                .HasPrecision(9, 6);

            builder.Property(x => x.Longitude)
                .HasPrecision(9, 6);

            builder.Property(x => x.CreatedAtUtc)
             .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.Property)
                .WithOne(x => x.Address)
                .HasForeignKey<PropertyAddress>(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Suburb);
            builder.HasIndex(x => x.State);
            builder.HasIndex(x => x.Postcode);
        }
    }
}
