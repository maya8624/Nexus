using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Domain.Entities
{
    public class Agency
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = default!;

        public string? Abn { get; set; }

        public string? LicenseNumber { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? WebsiteUrl { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public ICollection<Agent> Agents { get; set; } = new List<Agent>();

        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    }
}
