using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;

namespace Nexus.Application.Interfaces.Business
{
    public interface IInspectionBookingService
    {
        Task<Result<InspectionBookingDto>> CreateAsync(InspectionBookingRequest request, Guid userId, CancellationToken ct);
        Task<Result<IReadOnlyList<InspectionBookingDto>>> GetMyBookingsAsync(Guid userId, CancellationToken ct);
        Task<Result<InspectionBookingDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
        Task<Result<bool>> CancelAsync(Guid id, Guid userId, CancellationToken ct);
    }
}
