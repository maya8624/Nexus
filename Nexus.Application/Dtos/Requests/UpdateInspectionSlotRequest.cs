namespace Nexus.Application.Dtos.Requests
{
    public sealed class UpdateInspectionSlotRequest
    {
        public Guid AgentId { get; init; }
        public DateTimeOffset StartAtUtc { get; init; }
        public DateTimeOffset EndAtUtc { get; init; }
        public int Capacity { get; init; }
        public string? Notes { get; init; }
    }
}
