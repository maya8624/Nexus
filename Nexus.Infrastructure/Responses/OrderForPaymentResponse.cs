using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Infrastructure.Responses
{
    public class OrderForPaymentResponse
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public Currency Currency { get; set; }
        public string FrontendIdempotencyKey { get; set; } = null!;
        public List<OrderItemForPaymentResponse> Items { get; set; } = new();
    }
}
