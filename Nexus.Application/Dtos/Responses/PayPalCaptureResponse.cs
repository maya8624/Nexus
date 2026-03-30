using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Responses
{
    public class PayPalCaptureResponse
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public List<PurchaseUnit> PurchaseUnits { get; set; }
    }

    public class PurchaseUnit
    {
        public PaymentDetails PaymentDetails { get; set; }
    }

    public class PaymentDetails
    {
        public List<Capture> Captures { get; set; }
    }

    public class Capture
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public Amount Amount { get; set; }
    }

    public class Amount
    {
        public string CurrencyCode { get; set; }
        public string Value { get; set; }
    }
}
