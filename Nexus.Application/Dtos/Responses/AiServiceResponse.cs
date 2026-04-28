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
        public required string reply { get; init; }
        public required string thread_id { get; init; }
        public string? property_id { get; init; }
        public List<PropertyListingResult> listings { get; set; }
    }

    public class PropertyListingResult
    {
        public string property_id { get; set; }
        public string property_url { get; set; }
        public string listing_Id { get; set; }

    }
}
