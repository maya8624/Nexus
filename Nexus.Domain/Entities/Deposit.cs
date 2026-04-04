using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities
{
    public class Deposit
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        
        public Guid PropertyId { get; set; }

        public Guid ListingId { get; set; }

        public decimal Amount { get; set; }

        public Currency Currency { get; set; } = Currency.AUD;

        public DepositStatus Status { get; set; } = DepositStatus.Pending;
        
        public string StripeSessionId { get; set; } = string.Empty;

        public string? StripeSessionUrl { get; set; }

        public string? StripePaymentIntentId { get; set; }
        
        public DateTimeOffset? PaidAtUtc { get; set; }

        public string IdempotencyKey { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        
        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        
        public string RawResponse { get; set; } = string.Empty;

        public User User { get; set; } = default!;
        
        public Property Property { get; set; } = default!;
        
        public Listing Listing { get; set; } = default!;
    }
}
