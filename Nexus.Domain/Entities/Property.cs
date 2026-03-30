using Nexus.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexus.Domain.Entities
{
    public class Property
    {
        public Guid Id { get; set; }

        public int PropertyTypeId { get; set; }

        public Guid? AgencyId { get; set; }

        public Guid? AgentId { get; set; }

        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        public int Bedrooms { get; set; }

        public int Bathrooms { get; set; }

        public int CarSpaces { get; set; }

        public decimal? LandSizeSqm { get; set; }

        public decimal? BuildingSizeSqm { get; set; }

        public int? YearBuilt { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public PropertyType PropertyType { get; set; } = default!;

        public Agency? Agency { get; set; }

        public Agent? Agent { get; set; }

        public PropertyAddress? Address { get; set; }

        public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();

        public ICollection<Listing> Listings { get; set; } = new List<Listing>();

        public ICollection<SavedProperty> SavedByUsers { get; set; } = new List<SavedProperty>();

        public ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();

        public ICollection<InspectionBooking> InspectionBookings { get; set; } = new List<InspectionBooking>();

        public ICollection<InspectionSlot> InspectionSlots { get; set; } = new List<InspectionSlot>();
    }
}