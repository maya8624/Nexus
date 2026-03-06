using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos
{
    public class PayPalRefundResponse
    {
        public string Id { get; set; } = default!;
        public string Status { get; set; } = default!;

        public PayPalAmount Amount { get; set; } = default!;
        public PayPalSellerPayableBreakdown? SellerPayableBreakdown { get; set; }

        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }

        public List<PayPalLink> Links { get; set; } = [];

        // 🔹 Internal use (not from PayPal schema)
        public string Raw { get; set; } = default!;
    }

    public class PayPalAmount
    {
        public string CurrencyCode { get; set; } = default!;
        public string Value { get; set; } = default!;
    }

    public class PayPalSellerPayableBreakdown
    {
        public PayPalAmount GrossAmount { get; set; } = default!;
        public PayPalAmount PayPalFee { get; set; } = default!;
        public PayPalAmount NetAmount { get; set; } = default!;
    }

}

