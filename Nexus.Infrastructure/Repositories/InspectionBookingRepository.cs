using Microsoft.EntityFrameworkCore;
using Nexus.Application.Dtos;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Infrastructure.Repositories
{
    public class InspectionBookingRepository : RepositoryBase<InspectionBooking>, IInspectionBookingRepository
    {
        private readonly AppDbContext _context;

        public InspectionBookingRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<InspectionBooking?> GetInspectionBookingById(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<InspectionBooking?> GetInspectionBookingForUpdate(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .Where(x => x.Id == id)
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> HasDuplicateBooking(InspectionBookingRequest request, Guid userId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Where(x => x.PropertyId == request.PropertyId)
                .Where(x => x.ListingId == request.ListingId)
                .Where(x => x.InspectionStartAtUtc == request.InspectionStartAtUtc)
                .Where(x => x.InspectionEndAtUtc == request.InspectionEndAtUtc)
                .Where(x => x.Status != request.Status) // not cancelled
                .AnyAsync(ct);
        }

        public async Task<bool> HasOverlappingConfirmedBooking(InspectionBookingRequest request, Guid userId, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Where(x => x.PropertyId == request.PropertyId)
                .Where(x => x.ListingId == request.ListingId)
                .Where(x => x.Status == InspectionBookingStatus.Confirmed)
                .Where(x => x.InspectionEndAtUtc.HasValue &&
                        request.InspectionStartAtUtc < x.InspectionEndAtUtc.Value &&
                        request.InspectionEndAtUtc > x.InspectionStartAtUtc)
                .AnyAsync(ct);
        }
    }
}
