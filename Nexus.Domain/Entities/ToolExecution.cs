using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class ToolExecution
    {
        public Guid Id { get; set; }

        public Guid ChatMessageId { get; set; }

        public string ToolName { get; set; } = default!;

        public string? InputJson { get; set; }

        public string? OutputJson { get; set; }

        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public ChatMessage ChatMessage { get; set; } = null!;
    }
}
