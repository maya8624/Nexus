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
    public class InspectionBookingConfiguration : IEntityTypeConfiguration<InspectionBooking>
    {
        public void Configure(EntityTypeBuilder<InspectionBooking> builder)
        {
            builder.ToTable("inspection_bookings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

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

            builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);

            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .IsRowVersion();

            builder.HasOne(x => x.InspectionSlot)
                .WithMany(x => x.InspectionBookings)
                .HasForeignKey(x => x.InspectionSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                .WithMany(x => x.InspectionBookings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Property)
                .WithMany(x => x.InspectionBookings)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Listing)
                .WithMany(x => x.InspectionBookings)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Agent)
                .WithMany(x => x.InspectionBookings)
                .HasForeignKey(x => x.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.AgentId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => new { x.InspectionSlotId, x.Status });
            builder.HasIndex(x => new { x.UserId, x.Status });
        }
    }
}
