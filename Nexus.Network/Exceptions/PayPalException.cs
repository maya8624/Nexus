
using Nexus.Network.Constants;

namespace Nexus.Application.Exceptions
{
    public class PayPalException : NetworkException
    {
        public override int StatusCode => NetworkStatusCodes.PayPalIssue;
        public override string Name => "PAYPAL_ISSUE";

        public PayPalException(string message) : base(message) { }
        public PayPalException(string message, Exception inner) : base(message, inner) { }
    }
}
