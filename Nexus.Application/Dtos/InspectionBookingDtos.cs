namespace Nexus.Application.Dtos
{
    public sealed class InspectionBookingDto
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        public Guid PropertyId { get; init; }

        public Guid ListingId { get; init; }

        public Guid? AgentId { get; init; }

        public DateTimeOffset InspectionStartAtUtc { get; init; }

        public DateTimeOffset InspectionEndAtUtc { get; init; }

        public string Status { get; init; } = string.Empty;

        public string? Notes { get; init; }

        public DateTimeOffset CreatedAtUtc { get; init; }

        public DateTimeOffset UpdatedAtUtc { get; init; }
    }
}
