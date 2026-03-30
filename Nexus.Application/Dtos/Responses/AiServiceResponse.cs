using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Responses
{
    /// <summary>
    /// Internal DTO representing the Python service response.
    /// Matches the Python FastAPI ChatResponse schema exactly.
    /// </summary>
    public class AiServiceResponse
    {
        public required string Reply { get; init; }
        public required string session_id { get; init; }
    }
}
