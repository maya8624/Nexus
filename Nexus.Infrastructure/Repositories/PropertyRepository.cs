using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace Nexus.Infrastructure.Repositories
{
    public class PropertyRepository : RepositoryBase<Property>, IPropertyRepository
    {
        private readonly AppDbContext _context;

        public PropertyRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<PropertyReadModel> Items, int TotalCount)> GetPagedProperties(
            int skip,
            int pageSize,
            int? propertyTypeId,
            CancellationToken ct)
        {
            var query = BuildBaseQuery();
            var totalCount = await query.CountAsync(ct);

            var items = await query
                .Select(ToReadModel)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<PropertyReadModel?> GetPropertyById(Guid id, CancellationToken ct)
        {
            return await BuildBaseQuery()
                .Where(x => x.Id == id)
                .Select(ToReadModel)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Property?> IsActiveGetBookingContext(Guid propertyId, Guid listingId, ListingStatus status, CancellationToken ct)
        {
            var result = await _context.Properties
                .AsNoTracking()
                .Include(x => x.Listings
                    .Where(l => l.Id == listingId)
                    .Where(l => l.IsPublished == true)
                    .Where(l => l.Status == status)
                    .FirstOrDefault())
                .Include(x => x.Agent)
                .Where(x => x.Id == propertyId)
                .Where(x => x.IsActive == true)
                .FirstOrDefaultAsync(ct);

            return result;

            //var result = await _context.Properties
            //    .AsNoTracking()
            //    .Where(x => x.Id == propertyId)
            //    .Select(x => new
            //    {
            //        PropertyId = x.Id,
            //        PropertyIsActive = x.IsActive,
            //        Listing = x.Listings
            //            .Where(l => l.Id == listingId)
            //            .Select(l => new
            //            {
            //                l.Id,
            //                l.IsPublished,
            //                Status = l.Status.ToString(),
            //                l.AgentId
            //            })
            //            .FirstOrDefault()
            //    })
            //    .Where(x => x.Listing != null)
            //    .Select(x => new BookingContextReadModel
            //    {
            //        PropertyId = x.,
            //        PropertyIsActive = x.PropertyIsActive,
            //        ListingId = x.Listing!.Id,
            //        ListingIsPublished = x.Listing.IsPublished,
            //        ListingStatus = x.Listing.Status,
            //        ListingAgentId = x.Listing.AgentId
            //    })
            //    .FirstOrDefaultAsync(ct);
        }

        private static readonly Expression<Func<Property, PropertyReadModel>> ToReadModel = x => new PropertyReadModel
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
        };

        private IQueryable<Property> BuildBaseQuery()
        {
            return _context.Properties
                .AsNoTracking()
                .Where(x => x.IsActive && x.Listings.Any(l => l.IsPublished));
        }
    }
}