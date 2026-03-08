using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = default!;

        public string? PasswordHash { get; set; }

        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset ModifiedAtUtc { get; set; }

        public ICollection<UserLogin> Logins { get; set; } = new List<UserLogin>();

        public ICollection<SavedProperty> SavedProperties { get; set; } = new List<SavedProperty>();

        public ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();

        public ICollection<InspectionBooking> InspectionBookings { get; set; } = new List<InspectionBooking>();

        public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    }


    //public class Transaction
    //{
    //    public Guid Id { get; set; } = Guid.NewGuid();
    //    public decimal Amount { get; set; }
    //    public string Currency { get; set; } = "USD";
    //    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    //    public string PaymentMethod { get; set; } = string.Empty;
    //    public string ReferenceNumber { get; set; } = string.Empty; // External ID from Stripe/PayPal

    //    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    //    // Foreign Key
    //    public Guid UserId { get; set; }
    //    public User User { get; set; } = null!;
    //}
}
