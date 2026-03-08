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
    public class PropertyConfiguration : IEntityTypeConfiguration<Property>
    {
        public void Configure(EntityTypeBuilder<Property> builder)
        {
            builder.ToTable("properties");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Description);

            builder.Property(x => x.Bedrooms)
                .IsRequired();

            builder.Property(x => x.Bathrooms)
                .IsRequired();

            builder.Property(x => x.CarSpaces)
                .IsRequired();

            builder.Property(x => x.LandSizeSqm)
                .HasPrecision(10, 2);

            builder.Property(x => x.BuildingSizeSqm)
                .HasPrecision(10, 2);

            builder.Property(x => x.YearBuilt);

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.HasOne(x => x.PropertyType)
                .WithMany(x => x.Properties)
                .HasForeignKey(x => x.PropertyTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Agency)
                .WithMany()
                .HasForeignKey(x => x.AgencyId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Agent)
                .WithMany()
                .HasForeignKey(x => x.AgentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Address)
                .WithOne(x => x.Property)
                .HasForeignKey<PropertyAddress>(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.PropertyTypeId);
            builder.HasIndex(x => x.AgencyId);
            builder.HasIndex(x => x.AgentId);
            builder.HasIndex(x => x.IsActive);
        }
    }
}
