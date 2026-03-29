using Nexus.Application.Dtos;

namespace Nexus.Application.Interfaces.Business
{
    public interface IPropertyService
    {
        Task<PropertyListResponse> GetProperties(PropertyQueryRequest request, CancellationToken ct);
        Task<PropertyDto?> GetPropertyById(Guid id, CancellationToken ct);
    }
}
