using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class ChatSession
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string? Title { get; set; }

        public DateTimeOffset StartedAtUtc { get; set; }

        public DateTimeOffset? EndedAtUtc { get; set; }

        public User User { get; set; } = null!;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();       
    }
}
