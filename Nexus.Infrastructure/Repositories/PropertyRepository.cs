using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;
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

        public async Task<(IReadOnlyList<PropertyReadModel> Items, int TotalCount)> GetPagedAsync(
            int skip,
            int pageSize,
            int? propertyTypeId,
            CancellationToken ct)
        {
            var query = BuildBaseQuery(propertyTypeId);
            var totalCount = await query.CountAsync(ct);

            var items = await query
                .Select(ToReadModel)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<PropertyReadModel?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await BuildBaseQuery(propertyTypeId: null)
                .Where(x => x.Id == id)
                .Select(ToReadModel)
                .FirstOrDefaultAsync(ct);
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

        private IQueryable<Property> BuildBaseQuery(int? propertyTypeId)
        {
            var query = _context.Properties
                .AsNoTracking()
                .Where(x => x.IsActive && x.Listings.Any(l => l.IsPublished));

            if (propertyTypeId.HasValue)
                query = query.Where(x => x.PropertyTypeId == propertyTypeId.Value);

            return query;
        }
    }
}
