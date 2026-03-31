using Nexus.Application.Dtos.Requests;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IInspectionSlotRepository : IRepositoryBase<InspectionSlot>
    {
        Task<InspectionSlot?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);
        Task<bool> HasConflictingSlotAsync(Guid propertyId, Guid agentId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken ct, Guid? excludeId = null);
        Task<IReadOnlyList<AvailableInspectionSlotReadModel>> GetAvailableSlotsAsync(
            GetAvailableInspectionSlotsRequest request,
            CancellationToken ct);
    }
}