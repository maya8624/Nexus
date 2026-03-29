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
    public class ListingConfiguration : IEntityTypeConfiguration<Listing>
    {
        public void Configure(EntityTypeBuilder<Listing> builder)
        {
            builder.ToTable("listings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.ListingType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.ListedAtUtc)
                .IsRequired();

            builder.Property(x => x.AvailableFromUtc);

            builder.Property(x => x.ClosedAtUtc);

            builder.Property(x => x.IsPublished)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
             .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.Property)
                .WithMany(x => x.Listings)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Agent)
               .WithMany(x => x.Listings)   
               .HasForeignKey(x => x.AgentId)
               .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Agency)
                .WithMany(x => x.Listings)
                .HasForeignKey(x => x.AgencyId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.PropertyId);
            builder.HasIndex(x => x.AgencyId);
            builder.HasIndex(x => x.AgentId);
            builder.HasIndex(x => x.ListingType);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.IsPublished);
            builder.HasIndex(x => x.ListedAtUtc);
        }
    }
}
