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

            builder.Property(x => x.InspectionStartAtUtc)
                .IsRequired();

            builder.Property(x => x.InspectionEndAtUtc);

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

            builder.HasOne(x => x.User)
                .WithMany(x => x.InspectionBookings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Property)
                .WithMany(x => x.InspectionBookings)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Listing)
                .WithMany(x => x.InspectionBookings)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Agent)
                .WithMany(x => x.InspectionBookings)
                .HasForeignKey(x => x.AgentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.PropertyId);
            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => x.AgentId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.InspectionStartAtUtc);
        }
    }
}
