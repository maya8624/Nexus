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
    public class SavedPropertyConfiguration : IEntityTypeConfiguration<SavedProperty>
    { 
        public void Configure(EntityTypeBuilder<SavedProperty> builder)
        {
            builder.ToTable("saved_properties");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.SavedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany(x => x.SavedProperties)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Property)
                .WithMany(x => x.SavedByUsers)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.PropertyId);

            builder.HasIndex(x => new { x.UserId, x.PropertyId })
                .IsUnique();
        }
    }
}

