using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Responses
{
    public class PayPalOrderResponse
    {
        public string Id { get; set; } = null!;
        public string? Status { get; set; }
        public List<PayPalLink>? Links { get; set; }
    }

    public class PayPalLink
    {
        public string Href { get; set; } = null!;
        public string Rel { get; set; } = null!;
        public string Method { get; set; } = null!;
    }
}
