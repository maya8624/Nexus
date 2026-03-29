using Nexus.Application.Constants;

namespace Nexus.Application.Exceptions
{
    public class NotFoundException : AppException
    {
        public override int StatusCode => CustomStatusCodes.NotFound;
        public override string Name => "NOT_FOUND";

        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}
