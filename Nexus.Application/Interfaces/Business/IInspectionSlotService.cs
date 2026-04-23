using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;

namespace Nexus.Application.Interfaces.Business
{
    public interface IInspectionSlotService
    {
        Task<Result<InspectionSlotDto>> CreateAsync(InspectionSlotRequest request, CancellationToken ct);
        Task<Result<IReadOnlyList<InspectionSlotDto>>> GetAvailableSlotsAsync(Guid propertyId, CancellationToken ct);
        Task<Result<InspectionSlotDto>> GetInspectionSlotByIdAsync(Guid id, CancellationToken ct);
        Task<Result<InspectionSlotDto>> Update(Guid id, UpdateInspectionSlotRequest request, CancellationToken ct);
        Task<Result<InspectionSlotDto>> Cancel(Guid id, CancellationToken ct);
    }
}
