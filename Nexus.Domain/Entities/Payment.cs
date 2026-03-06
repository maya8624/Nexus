using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public PaymentProvider Provider { get; set; } 
        public string ProviderOrderId { get; set; } = default!;
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string ProviderCaptureId { get; set; }
        public decimal Amount { get; set; }
        public decimal CapturedAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public string BackendIdempotencyKey { get; set; }
        public Currency Currency { get; set; } = Currency.AUD;
        public DateTimeOffset? CapturedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string RawResponse { get; set; }

        public Order Order { get; set; } = default!;
        public ICollection<Refund> Refunds { get; set; } = [];
        public decimal RefundableAmount => CapturedAmount - RefundedAmount;
    }
}
