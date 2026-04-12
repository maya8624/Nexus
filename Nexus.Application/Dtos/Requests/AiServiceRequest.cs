using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Requests
{
    /// <summary>
    /// Internal DTO used by AiService to communicate with Python service.
    /// Matches the Python FastAPI ChatRequest schema exactly.
    /// </summary>
    public class AiServiceRequest
    {
        public required string Message { get; init; }

        // snake_case to match Python FastAPI schema
        public required string thread_id { get; init; }
        public string? property_id { get; init; }
    }
}
