using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;

namespace Nexus.Application.Interfaces.Business
{
    public interface IInspectionBookingService
    {
        Task<Result<InspectionBookingDto>> CreateAsync(InspectionBookingRequest request, CancellationToken ct);
        Task<Result<IReadOnlyList<InspectionBookingDto>>> GetMyBookingsAsync(CancellationToken ct);
        Task<Result<InspectionBookingDto>> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Result<bool>> CancelAsync(Guid id, CancellationToken ct);
    }
}
