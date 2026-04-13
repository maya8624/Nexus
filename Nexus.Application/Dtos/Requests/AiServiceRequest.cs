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
        // snake_case to match Python FastAPI schema
        public required string message { get; init; }

        public required string thread_id { get; init; }
        public string? property_id { get; init; }
        public Guid user_id { get; init; }
        public bool is_new_conversation { get; init; }
    }
}
