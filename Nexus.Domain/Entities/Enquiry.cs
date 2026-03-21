using Nexus.Domain.Enums;
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

        public Guid? AgentId { get; set; }

        public string Message { get; set; } = default!;

        public EnquiryStatus Status { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public DateTimeOffset? RespondedAtUtc { get; set; }

        public User User { get; set; } = default!;

        public Property Property { get; set; } = default!;

        public Listing? Listing { get; set; }

        public Agent? Agent { get; set; }
    }
}
