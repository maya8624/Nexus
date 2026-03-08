using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class EnquiryConfiguration : IEntityTypeConfiguration<Enquiry>
    {
        public void Configure(EntityTypeBuilder<Enquiry> builder)
        {
            builder.ToTable("enquiries");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.Message)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.RespondedAtUtc);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Enquiries)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Property)
                .WithMany(x => x.Enquiries)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Listing)
                .WithMany(x => x.Enquiries)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Agent)
                .WithMany(x => x.Enquiries)
                .HasForeignKey(x => x.AgentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.PropertyId);
            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => x.AgentId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
