using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;
using Nexus.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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

            builder.Property(x => x.Body)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.DraftReply)
                .HasMaxLength(2000);

            builder.Property(x => x.SentReply)
                .HasMaxLength(2000);

            var draftSourcesProperty = builder.Property(x => x.DraftSources)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v) || v == "{}"
                    ? new List<SourceChunk>()
                    : JsonSerializer.Deserialize<List<SourceChunk>>(v, (JsonSerializerOptions?)null) ?? new List<SourceChunk>()
                )
                .IsRequired();

            draftSourcesProperty.Metadata.SetValueComparer(new ValueComparer<List<SourceChunk>>(
                (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                (v) => v == null ? 0 : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
                (v) => v == null ? new List<SourceChunk>() : JsonSerializer.Deserialize<List<SourceChunk>>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!
            ));

            builder.Property(x => x.Intent)
                .HasMaxLength(50);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
                .IsRequired();

            builder.Property(x => x.RepliedAtUtc);


            builder.HasOne(x => x.User)
                .WithMany(x => x.Enquiries)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Property)
                .WithMany(x => x.Enquiries)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Listing)
                .WithMany(x => x.Enquiries)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Agent)
                .WithMany(x => x.Enquiries)
                .HasForeignKey(x => x.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Tenant)
                .WithMany(x => x.Enquiries)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.PropertyId);
            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => x.AgentId);
            builder.HasIndex(x => x.TenantId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
