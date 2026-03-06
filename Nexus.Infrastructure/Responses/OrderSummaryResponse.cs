using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Infrastructure.Responses
{
    public class OrderSummaryResponse
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
