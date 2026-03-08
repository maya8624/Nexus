using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class InspectionBooking
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid PropertyId { get; set; }

        public Guid? ListingId { get; set; }

        public Guid? AgentId { get; set; }

        public DateTimeOffset InspectionStartAtUtc { get; set; }

        public DateTimeOffset? InspectionEndAtUtc { get; set; }

        public InspectionBookingStatus Status { get; set; }

        public string? Notes { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public User User { get; set; } = null!;

        public Property Property { get; set; } = null!;

        public Listing? Listing { get; set; }

        public Agent? Agent { get; set; }
    }
}
