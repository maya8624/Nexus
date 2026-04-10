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

        public Guid ListingId { get; set; }

        public Guid AgentId { get; set; }

        public Guid InspectionSlotId { get; set; }

        public InspectionBookingStatus Status { get; set; }

        public string? Notes { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public bool IsDeleted { get; set; }

        public User User { get; set; } = null!;

        public Property Property { get; set; } = null!;

        public Listing Listing { get; set; } = null!;

        public Agent Agent { get; set; } = null!;

        public InspectionSlot InspectionSlot { get; set; } = null!;
    }
}
