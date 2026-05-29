using Nexus.Domain.Enums;
using Nexus.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class Enquiry
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid PropertyId { get; set; }

        public Guid? ListingId { get; set; }

        public Guid? TenantId { get; set; }

        public Guid AgentId { get; set; }

        public string? DraftReply { get; set; }

        public string? SentReply { get; set; }

        public List<SourceChunk> DraftSources { get; set; } = [];

        public string Body { get; set; } = default!;

        public string? Intent { get; set; }

        public EnquiryStatus Status { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public DateTimeOffset? RepliedAtUtc { get; set; }

        public User User { get; set; } = default!;

        public Property Property { get; set; } = default!;

        public Listing? Listing { get; set; }

        public Agent Agent { get; set; } = default!;

        public Tenant? Tenant { get; set; }
    }
}
