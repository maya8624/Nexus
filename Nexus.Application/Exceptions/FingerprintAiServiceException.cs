using Microsoft.AspNetCore.Http;
using Nexus.Network.Constants;

namespace Nexus.Application.Exceptions
{
    /// <summary>
    /// Thrown when the Python AI service's fingerprint classification call fails.
    /// Caught by the global exception handler in API layer.
    /// </summary>
    public class FingerprintAiServiceException : NetworkException
    {
        public override int StatusCode => NetworkStatusCodes.AiServiceIssue;
        public override int HttpStatusCode => StatusCodes.Status503ServiceUnavailable;
        public override string Name => "FINGERPRINT_AI_SERVICE_ERROR";

        public FingerprintAiServiceException(string message) : base(message) { }
        public FingerprintAiServiceException(string message, Exception inner) : base(message, inner) { }
    }
}
