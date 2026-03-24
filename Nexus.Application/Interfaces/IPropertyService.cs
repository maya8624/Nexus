using Nexus.Application.Common;
using Nexus.Application.Dtos;

namespace Nexus.Application.Interfaces
{
    public interface IPropertyService
    {
        Task<PropertyListResponse> GetProperties(int page, int pageSize, string? type, CancellationToken ct);
        Task<PropertyDto?> GetPropertyById(Guid id, CancellationToken ct);
        Task<Result<InspectionBookingDto>> CreateInspectionBookingAsync(CreateInspectionBookingRequest request, CancellationToken ct);
        Task<Result<InspectionBookingDto>> CancelInspectionBookingAsync(Guid id, CancellationToken ct);
        Task<Result<InspectionBookingDto>> GetInspectionBookingByIdAsync(Guid id, CancellationToken ct);
        Task<Result<InspectionAvailabilityResponse>> CheckInspectionAvailabilityAsync(CheckInspectionAvailabilityRequest request, CancellationToken ct);
    }
}
