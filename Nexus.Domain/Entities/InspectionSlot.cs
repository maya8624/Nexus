using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class InspectionSlot
    {
        public Guid Id { get; set; }

        public Guid ListingId { get; set; }

        public Guid PropertyId { get; set; }

        public Guid AgentId { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset StartAtUtc { get; set; }

        public DateTimeOffset EndAtUtc { get; set; }

        public int Capacity { get; set; }

        public InspectionSlotStatus Status { get; set; }

        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public byte[] RowVersion { get; set; } = [];

        public Property Property { get; set; } = null!;

        public Listing Listing { get; set; } = null!;

        public Agent Agent { get; set; } = null!;

        public User User { get; set; } = null!;

        public ICollection<InspectionBooking> InspectionBookings { get; set; } = [];
    }
}
