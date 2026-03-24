using Microsoft.EntityFrameworkCore;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Interfaces;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Responses;
using PropertyTypeEnum = Nexus.Domain.Enums.PropertyType;

namespace Nexus.Infrastructure.Repositories
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly AppDbContext _context;

        public PropertyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<PropertyReadModel> Items, int TotalCount)> GetPagedProperties(
            int page,
            int pageSize,
            PropertyTypeEnum? type,
            CancellationToken ct)
        {
            var query = BuildBaseQuery();

            if (type.HasValue)
            {
                query = query.Where(x => x.PropertyTypeId == (int)type.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.Listings
                    .Where(l => l.IsPublished)
                    .Select(l => (DateTimeOffset?)l.ListedAtUtc)
                    .Max())
                .ThenBy(x => x.Title)
                .Select(x => new PropertyReadModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    AddressLine1 = x.Address != null ? x.Address.AddressLine1 : null,
                    AddressLine2 = x.Address != null ? x.Address.AddressLine2 : null,
                    Suburb = x.Address != null ? x.Address.Suburb : string.Empty,
                    State = x.Address != null ? x.Address.State : string.Empty,
                    Postcode = x.Address != null ? x.Address.Postcode : string.Empty,
                    PriceValue = x.Listings
                        .Where(l => l.IsPublished)
                        .OrderByDescending(l => l.ListedAtUtc)
                        .Select(l => l.Price)
                        .FirstOrDefault(),
                    PropertyType = x.PropertyType != null ? x.PropertyType.Name : string.Empty,
                    Bedrooms = x.Bedrooms,
                    Bathrooms = x.Bathrooms,
                    Parking = x.CarSpaces,
                    LandSizeSqm = x.LandSizeSqm,
                    Description = x.Description ?? string.Empty,
                    Images = x.Images
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .ToList(),
                    AgentFirstName = x.Agent != null ? x.Agent.FirstName : string.Empty,
                    AgentLastName = x.Agent != null ? x.Agent.LastName : string.Empty,
                    AgentPhone = x.Agent != null ? x.Agent.PhoneNumber ?? string.Empty : string.Empty,
                    AgentPhoto = x.Agent != null ? x.Agent.PhotoUrl ?? string.Empty : string.Empty,
                    AgencyName = x.Agency != null ? x.Agency.Name : string.Empty,
                    ListedAtUtc = x.Listings
                        .Where(l => l.IsPublished)
                        .OrderByDescending(l => l.ListedAtUtc)
                        .Select(l => (DateTimeOffset?)l.ListedAtUtc)
                        .FirstOrDefault()
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<PropertyReadModel?> GetPropertyById(Guid id, CancellationToken ct)
        {
            return await BuildBaseQuery()
                .Where(x => x.Id == id)
                .Select(x => new PropertyReadModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    AddressLine1 = x.Address != null ? x.Address.AddressLine1 : null,
                    AddressLine2 = x.Address != null ? x.Address.AddressLine2 : null,
                    Suburb = x.Address != null ? x.Address.Suburb : string.Empty,
                    State = x.Address != null ? x.Address.State : string.Empty,
                    Postcode = x.Address != null ? x.Address.Postcode : string.Empty,
                    PriceValue = x.Listings
                        .Where(l => l.IsPublished)
                        .OrderByDescending(l => l.ListedAtUtc)
                        .Select(l => l.Price)
                        .FirstOrDefault(),
                    PropertyType = x.PropertyType != null ? x.PropertyType.Name : string.Empty,
                    Bedrooms = x.Bedrooms,
                    Bathrooms = x.Bathrooms,
                    Parking = x.CarSpaces,
                    LandSizeSqm = x.LandSizeSqm,
                    Description = x.Description ?? string.Empty,
                    Images = x.Images
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .ToList(),
                    AgentFirstName = x.Agent != null ? x.Agent.FirstName : string.Empty,
                    AgentLastName = x.Agent != null ? x.Agent.LastName : string.Empty,
                    AgentPhone = x.Agent != null ? x.Agent.PhoneNumber ?? string.Empty : string.Empty,
                    AgentPhoto = x.Agent != null ? x.Agent.PhotoUrl ?? string.Empty : string.Empty,
                    AgencyName = x.Agency != null ? x.Agency.Name : string.Empty,
                    ListedAtUtc = x.Listings
                        .Where(l => l.IsPublished)
                        .OrderByDescending(l => l.ListedAtUtc)
                        .Select(l => (DateTimeOffset?)l.ListedAtUtc)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> UserExists(Guid userId, CancellationToken ct)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.Id == userId && x.IsActive, ct);
        }

        public async Task<bool> AgentExists(Guid agentId, CancellationToken ct)
        {
            return await _context.Agencts
                .AsNoTracking()
                .AnyAsync(x => x.Id == agentId && x.IsActive, ct);
        }

        public async Task<BookingContextReadModel?> GetBookingContext(Guid propertyId, Guid listingId, CancellationToken ct)
        {
            return await _context.Properties
                .AsNoTracking()
                .Where(x => x.Id == propertyId)
                .Select(x => new BookingContextReadModel
                {
                    PropertyId = x.Id,
                    PropertyIsActive = x.IsActive,
                    ListingId = x.Listings
                        .Where(l => l.Id == listingId)
                        .Select(l => l.Id)
                        .FirstOrDefault(),
                    ListingIsPublished = x.Listings
                        .Where(l => l.Id == listingId)
                        .Select(l => l.IsPublished)
                        .FirstOrDefault(),
                    ListingStatus = x.Listings
                        .Where(l => l.Id == listingId)
                        .Select(l => l.Status.ToString())
                        .FirstOrDefault() ?? string.Empty,
                    ListingAgentId = x.Listings
                        .Where(l => l.Id == listingId)
                        .Select(l => l.AgentId)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(x => x.ListingId != Guid.Empty, ct);
        }

        public async Task<bool> HasDuplicateBooking(
            Guid userId,
            Guid propertyId,
            Guid listingId,
            DateTimeOffset inspectionStartAtUtc,
            DateTimeOffset inspectionEndAtUtc,
            CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.PropertyId == propertyId &&
                    x.ListingId == listingId &&
                    x.InspectionStartAtUtc == inspectionStartAtUtc &&
                    x.InspectionEndAtUtc == inspectionEndAtUtc &&
                    x.Status != InspectionBookingStatus.Cancelled,
                    ct);
        }

        public async Task<bool> HasOverlappingConfirmedBooking(
            Guid propertyId,
            DateTimeOffset inspectionStartAtUtc,
            DateTimeOffset inspectionEndAtUtc,
            Guid? excludeBookingId,
            CancellationToken ct)
        {
            return await _context.InspectionBookings
                .AsNoTracking()
                .AnyAsync(x =>
                    x.PropertyId == propertyId &&
                    x.Status == InspectionBookingStatus.Confirmed &&
                    (excludeBookingId == null || x.Id != excludeBookingId.Value) &&
                    x.InspectionEndAtUtc.HasValue &&
                    inspectionStartAtUtc < x.InspectionEndAtUtc.Value &&
                    inspectionEndAtUtc > x.InspectionStartAtUtc,
                    ct);
        }

        public async Task AddInspectionBooking(InspectionBooking booking, CancellationToken ct)
        {
            await _context.InspectionBookings.AddAsync(booking, ct);
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

        private IQueryable<Property> BuildBaseQuery()
        {
            return _context.Properties
                .AsNoTracking()
                .Include(x => x.Address)
                .Include(x => x.Images)
                .Include(x => x.Agent)
                .Include(x => x.Agency)
                .Include(x => x.PropertyType)
                .Include(x => x.Listings)
                .Where(x => x.IsActive && x.Listings.Any(l => l.IsPublished));
        }
    }
}
