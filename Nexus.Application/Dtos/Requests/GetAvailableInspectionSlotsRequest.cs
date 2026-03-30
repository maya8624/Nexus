using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Requests
{
    public class GetAvailableInspectionSlotsRequest
    {
        public Guid ListingId { get; init; }
        public DateTimeOffset? FromUtc { get; init; }
        public DateTimeOffset? ToUtc { get; init; }
        public int? Limit { get; init; }
    }
}