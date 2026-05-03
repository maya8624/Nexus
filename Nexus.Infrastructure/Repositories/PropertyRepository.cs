using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class PropertyRepository : RepositoryBase<Property>, IPropertyRepository
    {
        private readonly AppDbContext _context;

        public PropertyRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<PropertyReadModel> Items, int TotalCount)> GetPagedAsync(
            int skip,
            int pageSize,
            int? propertyTypeId,
            ListingType? listingType,
            CancellationToken ct)
        {
            var query = BuildBaseQuery(propertyTypeId, listingType);
            var totalCount = await query.CountAsync(ct);

            var items = await ToReadModel(query)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<PropertyReadModel?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await ToReadModel(BuildBaseQuery(propertyTypeId: null).Where(x => x.Id == id))
                .FirstOrDefaultAsync(ct);
        }

        private static IQueryable<PropertyReadModel> ToReadModel(IQueryable<Property> source) =>
            from x in source
            let listing = x.Listings
                .Where(l => l.IsPublished)
                .OrderByDescending(l => l.ListedAtUtc)
                .FirstOrDefault()
            select new PropertyReadModel
            {
                Id = x.Id,
                Title = x.Title,
                AddressLine1 = x.Address != null ? x.Address.AddressLine1 : null,
                AddressLine2 = x.Address != null ? x.Address.AddressLine2 : null,
                Suburb = x.Address != null ? x.Address.Suburb : string.Empty,
                State = x.Address != null ? x.Address.State : string.Empty,
                Postcode = x.Address != null ? x.Address.Postcode : string.Empty,
                PriceValue = listing != null ? listing.Price : 0,
                ListingType = listing != null ? (ListingType?)listing.ListingType : null,
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
                AgentFirstName = listing != null && listing.Agent != null ? listing.Agent.FirstName : string.Empty,
                AgentLastName = listing != null && listing.Agent != null ? listing.Agent.LastName : string.Empty,
                AgentPhone = listing != null && listing.Agent != null ? listing.Agent.PhoneNumber ?? string.Empty : string.Empty,
                AgentPhoto = listing != null && listing.Agent != null ? listing.Agent.PhotoUrl ?? string.Empty : string.Empty,
                AgencyName = listing != null && listing.Agency != null ? listing.Agency.Name : string.Empty,
                ListedAtUtc = listing != null ? (DateTimeOffset?)listing.ListedAtUtc : null,
            };

        private IQueryable<Property> BuildBaseQuery(int? propertyTypeId, ListingType? listingType = null)
        {
            var query = _context.Properties
                .AsNoTracking()
                .Where(x => x.IsActive && x.Listings.Any(l => l.IsPublished));

            if (propertyTypeId.HasValue)
                query = query.Where(x => x.PropertyTypeId == propertyTypeId.Value);

            if (listingType.HasValue)
                query = query.Where(x => x.Listings.Any(l => l.IsPublished && l.ListingType == listingType.Value));

            return query;
        }
    }
}
