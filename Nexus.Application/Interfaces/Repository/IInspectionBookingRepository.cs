using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IInspectionBookingRepository : IRepositoryBase<InspectionBooking>
    {
        Task<InspectionBooking?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
        Task<InspectionBooking?> GetByIdForUpdateAsync(Guid id, Guid userId, CancellationToken ct);
        Task<IReadOnlyList<InspectionBooking>> GetByUserIdAsync(Guid userId, CancellationToken ct);
        Task<bool> HasActiveBookingForSlotAsync(Guid slotId, Guid userId, CancellationToken ct);
        Task<bool> HasActiveBookingsAsync(Guid slotId, CancellationToken ct);
        Task<int> GetConfirmedCountForSlotAsync(Guid slotId, CancellationToken ct);
    }
}
