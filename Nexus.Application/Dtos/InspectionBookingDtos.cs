namespace Nexus.Application.Dtos
{
    public sealed class InspectionBookingDto
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public Guid PropertyId { get; init; }
        public string Status { get; init; } = string.Empty;
        public string AgentFirstName { get; set; } = string.Empty;
        public string AgentLastName { get; set; } = string.Empty;
        public string? AgentPhone { get; set; } = string.Empty;
        public DateTimeOffset StartAtUtc { get; init; }
        public DateTimeOffset EndAtUtc { get; init; }
    }
}
