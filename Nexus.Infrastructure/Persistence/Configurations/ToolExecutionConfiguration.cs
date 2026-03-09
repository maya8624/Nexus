using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Nexus.Domain.Entities;

    public class ToolExecutionConfiguration : IEntityTypeConfiguration<ToolExecution>
    {
        public void Configure(EntityTypeBuilder<ToolExecution> builder)
        {
            builder.ToTable("tool_executions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.ToolName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.InputJson);

            builder.Property(x => x.OutputJson);

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(2000);

            builder.Property(x => x.Success)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.ChatMessage)
                .WithMany(x => x.ToolExecutions)
                .HasForeignKey(x => x.ChatMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ChatMessageId);
            builder.HasIndex(x => x.ToolName);
            builder.HasIndex(x => x.Success);
            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
