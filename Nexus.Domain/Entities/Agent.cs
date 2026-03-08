using System.ComponentModel.DataAnnotations;

namespace Nexus.Domain.Entities
{
    public class Agent
    {
        public Guid Id { get; set; }

        public Guid? AgencyId { get; set; }

        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? LicenseNumber { get; set; }

        public string? PositionTitle { get; set; }

        public string? Bio { get; set; }

        public string? PhotoUrl { get; set; }

        public bool IsActive { get; set; }

        public Agency? Agency { get; set; }

        public ICollection<Property> Properties { get; set; } = new List<Property>();

        public ICollection<Listing> Listings { get; set; } = new List<Listing>();

        public ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();

        public ICollection<InspectionBooking> InspectionBookings { get; set; } = new List<InspectionBooking>();
    }
}