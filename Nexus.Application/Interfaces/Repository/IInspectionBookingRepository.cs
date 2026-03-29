using Nexus.Application.Dtos;
using Nexus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IInspectionBookingRepository : IRepositoryBase<InspectionBooking>
    {
        Task<bool> HasDuplicateBooking(CreateInspectionBookingRequest request, CancellationToken ct);
        Task<bool> HasOverlappingConfirmedBooking(CreateInspectionBookingRequest request, CancellationToken ct);
        Task<InspectionBooking?> GetInspectionBookingById(Guid id, CancellationToken ct);
        Task<InspectionBooking?> GetInspectionBookingForUpdate(Guid id, CancellationToken ct);
    }
}
