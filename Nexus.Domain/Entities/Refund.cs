using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class Refund
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public PaymentProvider Provider { get; set; }
        public string ProviderRefundId { get; set; }
        public string BackendIdempotencyKey { get; set; }
        public decimal Amount { get; set; }
        public RefundStatus Status { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public string RawResponse { get; set; }
        public Payment Payment { get; set; }
    }
}
