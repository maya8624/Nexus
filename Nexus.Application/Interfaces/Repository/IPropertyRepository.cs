using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IPropertyRepository : IRepositoryBase<Property>
    {
        Task<(IReadOnlyList<PropertyReadModel> Items, int TotalCount)> GetPagedAsync(
            int skip,
            int pageSize,
            int? propertyTypeId,
            ListingType? listingType,
            CancellationToken ct);

        Task<PropertyReadModel?> GetByIdAsync(Guid id, CancellationToken ct);
    }
}
