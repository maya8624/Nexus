using Nexus.Application.Constants;

namespace Nexus.Application.Exceptions
{
    public class RefundException : AppException
    {
        public override int StatusCode => CustomStatusCodes.RefundIssue;
        public override string Name => "REFUND_ISSUE";

        public RefundException(string message) : base(message) { }
        public RefundException(string message, Exception inner) : base(message, inner) { }
    }
}
