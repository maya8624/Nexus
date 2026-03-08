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
    public class PropertyImagesConfiguration : IEntityTypeConfiguration<PropertyImage>
    {
        public void Configure(EntityTypeBuilder<PropertyImage> builder)
        {
            builder.ToTable("property_images");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(x => x.PropertyId)
                .HasColumnName("property_id")
                .IsRequired();

            builder.Property(x => x.ImageUrl)
                .HasColumnName("image_url")
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.Caption)
                .HasColumnName("caption")
                .HasMaxLength(300);

            builder.Property(x => x.DisplayOrder)
                .HasColumnName("display_order")
                .IsRequired();

            builder.Property(x => x.IsPrimary)
                .HasColumnName("is_primary")
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasIndex(x => x.PropertyId)
                .HasDatabaseName("ix_property_images_property_id");

            builder.HasIndex(x => new { x.PropertyId, x.DisplayOrder })
                .HasDatabaseName("ux_property_images_property_id_display_order")
                .IsUnique();

            builder.HasIndex(x => x.PropertyId)
                .HasDatabaseName("ux_property_images_primary_per_property")
                .IsUnique()
                .HasFilter("is_primary = true");

            builder.HasOne(x => x.Property)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
