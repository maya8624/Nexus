using Nexus.Application.Dtos.Requests;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IInspectionSlotRepository : IRepositoryBase<InspectionSlot>
    {
        // Public read — no userId filter. When agent dashboard or role-based access is needed,
        // add GetByIdAsync(Guid id, Guid userId) or GetSlotsByAgentIdAsync(Guid agentId).
        Task<InspectionSlot?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<InspectionSlot?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);
        Task<bool> HasConflictingSlotAsync(InspectionSlotRequest request, CancellationToken ct, Guid? excludeId = null);
        Task<IReadOnlyList<AvailableInspectionSlotReadModel>> GetAvailableSlotsAsync(
            GetAvailableInspectionSlotsRequest request,
            CancellationToken ct);
    }
}