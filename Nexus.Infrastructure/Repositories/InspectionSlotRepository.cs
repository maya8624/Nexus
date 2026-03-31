using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class InspectionSlotRepository : RepositoryBase<InspectionSlot>, IInspectionSlotRepository
    {
        private readonly AppDbContext _context;

        public InspectionSlotRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<InspectionSlot?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.InspectionSlots
                .AsNoTracking()
                .Where(x => x.Id == id && x.IsDeleted == false)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<InspectionSlot?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
        {
            return await _context.InspectionSlots
                .Where(x => x.Id == id && x.IsDeleted == false)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> HasConflictingSlotAsync(Guid propertyId, Guid agentId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken ct, Guid? excludeId = null)
        {
            return await _context.InspectionSlots
                .AsNoTracking()
                .Where(x =>
                    x.PropertyId == propertyId &&
                    x.AgentId == agentId &&
                    x.IsDeleted == false &&
                    x.Status != InspectionSlotStatus.Cancelled &&
                    x.StartAtUtc < endAtUtc &&
                    x.EndAtUtc > startAtUtc &&
                    (excludeId == null || x.Id != excludeId))
                .AnyAsync(ct);
        }

        public async Task<IReadOnlyList<AvailableInspectionSlotReadModel>> GetAvailableSlotsAsync(
            GetAvailableInspectionSlotsRequest request,
            CancellationToken ct)
        {
            var query = _context.InspectionSlots
                .AsNoTracking()
                .Where(x =>
                    x.ListingId == request.ListingId &&
                    x.IsDeleted == false &&
                    x.Status == InspectionSlotStatus.Open &&
                    x.StartAtUtc >= request.FromUtc &&
                    x.StartAtUtc <= request.ToUtc &&
                    x.Listing.IsDeleted == false &&
                    x.Listing.Status == ListingStatus.Active)
                .Select(x => new
                {
                    Slot = x,
                    ActiveBookingCount = x.InspectionBookings.Count(b =>
                        b.IsDeleted == false &&
                        b.Status == InspectionBookingStatus.Confirmed)
                })
                .Where(x => x.ActiveBookingCount < x.Slot.Capacity)
                .OrderBy(x => x.Slot.StartAtUtc)
                .Take(request.Limit!.Value)
                .Select(x => new AvailableInspectionSlotReadModel
                {
                    InspectionSlotId = x.Slot.Id,
                    ListingId = x.Slot.ListingId,
                    PropertyId = x.Slot.PropertyId,
                    AgentId = x.Slot.AgentId,
                    StartAtUtc = x.Slot.StartAtUtc,
                    EndAtUtc = x.Slot.EndAtUtc,
                    Capacity = x.Slot.Capacity,
                    Status = x.Slot.Status,
                    Notes = x.Slot.Notes,
                    ActiveBookingCount = x.ActiveBookingCount,
                    RemainingCapacity = x.Slot.Capacity - x.ActiveBookingCount
                });

            return await query.ToListAsync(ct);
        }
    }
}