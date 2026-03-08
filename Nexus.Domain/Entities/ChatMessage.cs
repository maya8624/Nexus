using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{

    /// <summary>
    /// Represents a single chat message in a conversation session.
    /// Kept in Domain as it's a core business entity.
    /// </summary>
    public class ChatMessage
    {
        public Guid Id { get; private set; }
        public string SessionId { get; private set; }
        public string UserId { get; private set; }
        public string Message { get; private set; }
        public string? Reply { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private ChatMessage() { } // EF Core constructor

        public static ChatMessage Create(string sessionId, string userId, string message)
        {
            return new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
            };
        }

        public void SetReply(string reply)
        {
            Reply = reply;
        }
    }
}
