using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class DocumentSuggestion
    {
        public Guid Id { get; set; }
        public Guid DocId { get; set; }
        public Guid UserId { get; set; }
        public List<string> Suggestions { get; set; } = [];
        public DateTimeOffset CreatedAtUtc { get; set; }
        public string? ModelUsed { get; set; }
        public User User { get; set; } = default!;
    }
}