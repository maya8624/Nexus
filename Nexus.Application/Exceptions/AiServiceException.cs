using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Exceptions
{
    /// <summary>
    /// Thrown when the Python AI service call fails.
    /// Caught by the global exception handler in API layer.
    /// </summary>
    public class AiServiceException : Exception
    {
        public AiServiceException(string message) : base(message) { }
        public AiServiceException(string message, Exception inner) : base(message, inner) { }
    }
}
