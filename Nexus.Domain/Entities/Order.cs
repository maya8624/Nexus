using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public Currency Currency { get; set; } = Currency.AUD;
        public string FrontendIdempotencyKey { get; set; } 
        public int UserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<OrderItem> Items { get; set; } = [];
        public Payment Payment { get; set; }
    }
}
