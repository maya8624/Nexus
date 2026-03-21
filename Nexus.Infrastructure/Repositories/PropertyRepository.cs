using Microsoft.EntityFrameworkCore;
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

        private IQueryable<Domain.Entities.Property> BuildBaseQuery()
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
