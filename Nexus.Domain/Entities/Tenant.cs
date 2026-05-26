using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PropertyId { get; set; }
        public Guid? LeaseId { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Phone { get; set; }
        public DateOnly LeaseStartDate { get; set; }
        public DateOnly LeaseEndDate { get; set; }
        public decimal WeeklyRent { get; set; }
        public decimal BondAmount { get; set; }
        public TenantStatus Status { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public User User { get; set; } = default!;
        public Property Property { get; set; } = default!;
        public Lease? Lease { get; set; }
        public ICollection<Lease> Leases { get; set; } = [];
        public ICollection<Enquiry> Enquiries { get; set; } = [];
    }
}
