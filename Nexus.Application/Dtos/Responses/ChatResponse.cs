using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Responses
{
    public class ChatResponse
    {
        public required string Reply { get; init; }
        public required string ThreadId { get; init; }
        public string? PropertyId { get; init; }
        public IReadOnlyList<PropertyListing> Listings { get; set; }
    }

    public class PropertyListing
    {
        public string PropertyId { get; set; }
        public string PropertyUrl { get; set; }
        public string ListingId { get; set; }

    }
}
