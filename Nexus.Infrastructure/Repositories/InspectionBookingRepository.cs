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

        public async Task<InspectionBooking?> GetInspectionBookingById(Guid id, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<InspectionBooking?> GetInspectionBookingForUpdate(Guid id, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<bool> HasDuplicateBooking(CreateInspectionBookingRequest request, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .Where(x => x.UserId == request.UserId)
                .Where(x => x.PropertyId == request.PropertyId)
                .Where(x => x.ListingId == request.ListingId)
                .Where(x => x.InspectionStartAtUtc == request.InspectionStartAtUtc)
                .Where(x => x.InspectionEndAtUtc == request.InspectionEndAtUtc)
                .Where(x => x.Status != request.Status) // not cancelled
                .AnyAsync(ct);
        }

        public async Task<bool> HasOverlappingConfirmedBooking(CreateInspectionBookingRequest request, CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
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
