using PropertyTypeEnum = Nexus.Domain.Enums.PropertyType;
using Nexus.Infrastructure.Responses;

namespace Nexus.Infrastructure.Interfaces
{
    public interface IPropertyRepository
    {
        Task<(IReadOnlyList<PropertyReadModel> Items, int TotalCount)> GetPagedProperties(
            int page,
            int pageSize,
            PropertyTypeEnum? type,
            CancellationToken ct);

        Task<PropertyReadModel?> GetPropertyById(Guid id, CancellationToken ct);
    }
}
