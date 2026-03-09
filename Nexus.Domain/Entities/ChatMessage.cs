using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class ChatMessage
    {
        public Guid Id { get; set; }

        public Guid ChatSessionId { get; set; }

        public ChatMessageRole Role { get; set; }

        public string Content { get; set; } = default!;

        public string? ToolName { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public ChatSession ChatSession { get; set; } = null!;

        public ICollection<ToolExecution> ToolExecutions { get; set; } = new List<ToolExecution>();

        //public string? MetadataJson { get; set; }
        //public int? PromptTokens { get; set; }
        //public int? CompletionTokens { get; set; }
    }
}
