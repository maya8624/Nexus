using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Responses
{
    public class PayPalOrderResultResponse
    {
        public string PayPalOrderId { get; set; }
        public string ApproveUrl { get; set; }
    }
}
