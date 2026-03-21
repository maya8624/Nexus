using Nexus.Application.Dtos;

namespace Nexus.Application.Interfaces
{
    public interface IPropertyService
    {
        Task<PropertyListResponse> GetProperties(int page, int pageSize, string? type, CancellationToken ct);
        Task<PropertyDto?> GetPropertyById(Guid id, CancellationToken ct);
    }
}
