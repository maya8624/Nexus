using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class InspectionBookingRepository : RepositoryBase<InspectionBooking>, IInspectionBookingRepository
    {
        private readonly AppDbContext _context;

        public InspectionBookingRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<InspectionBooking?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .Include(x => x.InspectionSlot)
                .Include(x => x.Agent)
                .Where(x => x.Id == id && x.UserId == userId && x.IsDeleted == false)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<InspectionBooking?> GetByIdForUpdateAsync(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .Where(x => x.Id == id && x.UserId == userId && x.IsDeleted == false)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<InspectionBooking>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .Include(x => x.InspectionSlot)
                .Include(x => x.Agent)
                .Where(x => x.UserId == userId && x.IsDeleted == false && x.InspectionSlot.StartAtUtc > DateTimeOffset.UtcNow)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<bool> HasActiveBookingForSlotAsync(Guid slotId, Guid userId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .Where(x =>
                    x.InspectionSlotId == slotId &&
                    x.UserId == userId &&
                    x.IsDeleted == false &&
                    x.Status != InspectionBookingStatus.Cancelled)
                .AnyAsync(ct);
        }

        public async Task<bool> HasActiveBookingsAsync(Guid slotId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .Where(x =>
                    x.InspectionSlotId == slotId &&
                    x.IsDeleted == false &&
                    (x.Status == InspectionBookingStatus.Pending || x.Status == InspectionBookingStatus.Confirmed))
                .AnyAsync(ct);
        }

        public async Task<int> GetConfirmedCountForSlotAsync(Guid slotId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .Where(x =>
                    x.InspectionSlotId == slotId &&
                    x.IsDeleted == false &&
                    x.Status == InspectionBookingStatus.Confirmed)
                .CountAsync(ct);
        }
    }
}
