using Nexus.Application.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Exceptions
{
    public class PaymentException : AppException
    {
        public override int StatusCode => CustomStatusCodes.PaymentIssue;
        public override string Name => "PAYMENT_ISSue";

        public PaymentException(string message) : base(message) { }
        public PaymentException(string message, Exception inner) : base(message, inner) { }
    }
}
