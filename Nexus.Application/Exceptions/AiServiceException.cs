using Nexus.Network.Constants;

namespace Nexus.Application.Exceptions
{
    /// <summary>
    /// Thrown when the Python AI service call fails.
    /// Caught by the global exception handler in API layer.
    /// </summary>
    public class AiServiceException : NetworkException
    {
        public override int StatusCode => NetworkStatusCodes.AiServiceIssue;
        public override string Name => "AI_SERVICE_ERROR";

        public AiServiceException(string message) : base(message) { }
        public AiServiceException(string message, Exception inner) : base(message, inner) { }
    }
}
