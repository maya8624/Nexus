using Nexus.Application.Dtos.Requests;
using Nexus.Application.ReadModels;
using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IInspectionSlotRepository : IRepositoryBase<InspectionSlot>
    {
        Task<InspectionSlot?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);
        Task<bool> HasOverlappingSlotAsync(CreateInspectionSlotRequest request, CancellationToken ct);
        Task<bool> HasOverlappingSlotAsync(Guid propertyId, Guid agentId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken ct, Guid excludeId);
        Task<bool> HasActiveBookingsAsync(Guid slotId, CancellationToken ct);

        Task<IReadOnlyList<AvailableInspectionSlotReadModel>> GetAvailableSlotsAsync(
            Guid listingId,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            int limit,
            CancellationToken ct);
    }
}