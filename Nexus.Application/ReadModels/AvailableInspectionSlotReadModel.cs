using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Domain.Enums;

namespace Nexus.Application.ReadModels
{
    public sealed class AvailableInspectionSlotReadModel
    {
        public Guid InspectionSlotId { get; init; }
        public Guid ListingId { get; init; }
        public Guid PropertyId { get; init; }
        public Guid AgentId { get; init; }

        public DateTimeOffset StartAtUtc { get; init; }
        public DateTimeOffset EndAtUtc { get; init; }

        public int Capacity { get; init; }
        public int ActiveBookingCount { get; init; }
        public int RemainingCapacity { get; init; }

        public InspectionSlotStatus Status { get; init; }
        public string? Notes { get; init; }
    }
}