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
        Task<bool> HasDuplicateBooking(InspectionBookingRequest request, Guid userId, CancellationToken ct);
        Task<bool> HasOverlappingConfirmedBooking(InspectionBookingRequest request, Guid userId, CancellationToken ct);
        Task<InspectionBooking?> GetInspectionBookingById(Guid id, Guid userId, CancellationToken ct);
        Task<InspectionBooking?> GetInspectionBookingForUpdate(Guid id, Guid userId, CancellationToken ct);
    }
}
