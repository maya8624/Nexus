using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities
{
    public class Lease
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid PropertyId { get; set; }
        public Guid AgentId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal WeeklyRent { get; set; }
        public decimal BondAmount { get; set; }
        public LeaseType Type { get; set; }
        public LeaseStatus Status { get; set; }
        public bool WaterIncluded { get; set; }
        public decimal? WaterAllowanceLitresPerDay { get; set; }
        public DateOnly? VacatingDate { get; set; }
        public string? VacatingReason { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public Tenant Tenant { get; set; } = default!;
        public Property Property { get; set; } = default!;
        public Agent Agent { get; set; } = default!;
        public ICollection<Enquiry> Enquiries { get; set; } = [];
    }
}