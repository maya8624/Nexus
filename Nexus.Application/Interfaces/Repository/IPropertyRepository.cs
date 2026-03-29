using Nexus.Application.Dtos;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IPropertyRepository : IRepositoryBase<Property>
    {
        Task<(IReadOnlyList<PropertyReadModel> Items, int TotalCount)> GetPagedProperties(
            int page,
            int pageSize,
            int? propertyTypeId,
            CancellationToken ct
        );

        Task<PropertyReadModel?> GetPropertyById(Guid id, CancellationToken ct);
    }
}
