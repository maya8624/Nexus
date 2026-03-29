using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.ReadModels
{
    public sealed class BookingContextReadModel
    {
        public Guid PropertyId { get; init; }

        public bool PropertyIsActive { get; init; }

        public Guid ListingId { get; init; }

        public bool ListingIsPublished { get; init; }

        public string ListingStatus { get; init; } = string.Empty;

        public Guid? ListingAgentId { get; init; }
    }
}
