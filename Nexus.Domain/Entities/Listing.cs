using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class Listing
    {
        public Guid Id { get; set; }

        public Guid PropertyId { get; set; }

        public Guid? AgencyId { get; set; }

        public Guid? AgentId { get; set; }

        public ListingType ListingType { get; set; }

        public ListingStatus Status { get; set; }

        public decimal Price { get; set; }

        public DateTimeOffset ListedAtUtc { get; set; }

        public DateTimeOffset? AvailableFromUtc { get; set; }

        public DateTimeOffset? ClosedAtUtc { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public bool IsPublished { get; set; }

        public bool IsDeleted { get; set; }

        public Property Property { get; set; } = default!;

        public Agency? Agency { get; set; }

        public Agent? Agent { get; set; }

        public ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();

        public ICollection<InspectionBooking> InspectionBookings { get; set; } = new List<InspectionBooking>();

        public ICollection<InspectionSlot> InspectionSlots { get; set; } = new List<InspectionSlot>();
    }
}
