using Nexus.Application.Common;
using Nexus.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Interfaces.Business
{
    public interface IInspectionBookingService
    {
        Task<Result<InspectionBookingDto>> CreateInspectionBookingAsync(CreateInspectionBookingRequest request, CancellationToken ct);
        Task<Result<InspectionBookingDto>> CancelInspectionBookingAsync(Guid id, CancellationToken ct);
        Task<Result<InspectionBookingDto>> GetInspectionBookingByIdAsync(Guid id, CancellationToken ct);
        Task<Result<InspectionAvailabilityResponse>> CheckInspectionAvailabilityAsync(CheckInspectionAvailabilityRequest request, CancellationToken ct);
    }
}
