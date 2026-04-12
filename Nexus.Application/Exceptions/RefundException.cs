using Microsoft.AspNetCore.Http;
using Nexus.Application.Constants;

namespace Nexus.Application.Exceptions
{
    public class RefundException : AppException
    {
        public override int StatusCode => CustomStatusCodes.RefundIssue;
        public override int HttpStatusCode => StatusCodes.Status422UnprocessableEntity;
        public override string Name => "REFUND_ISSUE";

        public RefundException(string message) : base(message) { }
        public RefundException(string message, Exception inner) : base(message, inner) { }
    }
}
