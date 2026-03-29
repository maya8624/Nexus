using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.ReadModels
{
    public class OrderForPaymentReadModel
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public Currency Currency { get; set; }
        public string FrontendIdempotencyKey { get; set; } = null!;
        public List<OrderItemForPaymentReadModel> Items { get; set; } = new();
    }
}
