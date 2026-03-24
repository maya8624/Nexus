using Nexus.Infrastructure.Responses;
using Nexus.Domain.Entities;
using PropertyTypeEnum = Nexus.Domain.Enums.PropertyType;

namespace Nexus.Infrastructure.Interfaces
{
    public interface IPropertyRepository
    {
        Task<(IReadOnlyList<PropertyReadModel> Items, int TotalCount)> GetPagedProperties(
            int page,
            int pageSize,
            PropertyTypeEnum? type,
            CancellationToken ct);

        Task<PropertyReadModel?> GetPropertyById(Guid id, CancellationToken ct);
        Task<bool> UserExists(Guid userId, CancellationToken ct);
        Task<bool> AgentExists(Guid agentId, CancellationToken ct);
        Task<BookingContextReadModel?> GetBookingContext(Guid propertyId, Guid listingId, CancellationToken ct);
        Task<bool> HasDuplicateBooking(Guid userId, Guid propertyId, Guid listingId, DateTimeOffset inspectionStartAtUtc, DateTimeOffset inspectionEndAtUtc, CancellationToken ct);
        Task<bool> HasOverlappingConfirmedBooking(Guid propertyId, DateTimeOffset inspectionStartAtUtc, DateTimeOffset inspectionEndAtUtc, Guid? excludeBookingId, CancellationToken ct);
        Task AddInspectionBooking(InspectionBooking booking, CancellationToken ct);
        Task<InspectionBooking?> GetInspectionBookingById(Guid id, CancellationToken ct);
        Task<InspectionBooking?> GetInspectionBookingForUpdate(Guid id, CancellationToken ct);
    }

    public sealed class BookingContextReadModel
    {
        public Guid PropertyId { get; init; }

        public bool PropertyIsActive { get; init; }

        public Guid ListingId { get; init; }

        public bool ListingIsPublished { get; init; }

        public string ListingStatus { get; init; } = string.Empty;

        public Guid? ListingAgentId { get; init; }
    }
}
