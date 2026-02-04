using System;
using System.Collections.Generic;
using System.Text;

namespace NexusPay.Domain.Entities
{
    public class UserLogin
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = string.Empty; // "Google", "Microsoft", etc.
        public string ProviderKey { get; set; } = string.Empty; // The 'sub' ID from Google
        public Guid UserId { get; set; }
        public DateTimeOffset LastLoginAt { get; set; }
        public User User { get; set; } = null!; 
    }
}
