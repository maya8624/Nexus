using Nexus.Domain.Enums;

namespace Nexus.Application.Dtos.Responses
{
    public sealed class DepositResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PropertyId { get; set; }
        public Guid ListingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string StripeSessionId { get; set; } = string.Empty;
        public string? StripePaymentIntentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset? PaidAtUtc { get; set; }
        public string? SessionUrl { get; set; }
        public bool IsPaid => Status == DepositStatus.Pending.ToString() && StripePaymentIntentId != null && PaidAtUtc.HasValue;
    }
}
