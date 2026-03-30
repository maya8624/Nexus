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
    public class InspectionSlotConfiguration : IEntityTypeConfiguration<InspectionSlot>
    {
        public void Configure(EntityTypeBuilder<InspectionSlot> builder)
        {
            builder.ToTable("inspection_slots");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.StartAtUtc)
                .IsRequired();

            builder.Property(x => x.EndAtUtc)
                .IsRequired();

            builder.Property(x => x.Capacity)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
                .IsRequired();

            builder.Property(x => x.RowVersion)
                .IsRowVersion();

            builder.HasOne(x => x.Listing)
                .WithMany(x => x.InspectionSlots)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Property)
                .WithMany(x => x.InspectionSlots)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Agent)
                .WithMany(x => x.InspectionSlots)
                .HasForeignKey(x => x.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.InspectionBookings)
                .WithOne(x => x.InspectionSlot)
                .HasForeignKey(x => x.InspectionSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => x.PropertyId);
            builder.HasIndex(x => x.AgentId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.StartAtUtc);

            builder.HasIndex(x => new { x.ListingId, x.StartAtUtc });
            builder.HasIndex(x => new { x.PropertyId, x.StartAtUtc });
            builder.HasIndex(x => new { x.AgentId, x.StartAtUtc });
            builder.HasIndex(x => new { x.Status, x.StartAtUtc });

            builder.ToTable("inspection_slots", t =>
            {
                t.HasCheckConstraint(
                    "CK_inspection_slots_capacity_positive",
                    "\"Capacity\" > 0");

                t.HasCheckConstraint(
                    "CK_inspection_slots_end_after_start",
                    "\"EndAtUtc\" > \"StartAtUtc\"");
            });
        }
    }
}