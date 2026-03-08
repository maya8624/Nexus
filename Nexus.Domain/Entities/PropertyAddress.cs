using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class PropertyAddress
    {
        public Guid PropertyId { get; set; }

        public string AddressLine1 { get; set; } = default!;

        public string? AddressLine2 { get; set; }

        public string Suburb { get; set; } = default!;

        public string State { get; set; } = default!;

        public string Postcode { get; set; } = default!;

        public string Country { get; set; } = default!;

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public Property Property { get; set; } = default!;
    }
}
