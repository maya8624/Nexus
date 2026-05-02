using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;

namespace Nexus.Application.Interfaces.Business
{
    public interface IPropertyService
    {
        Task<Result<PropertyListResponse>> GetPropertiesAsync(PropertyQueryRequest request, CancellationToken ct);
        Task<Result<PropertyDto>> GetByIdAsync(Guid id, CancellationToken ct);
    }
}
