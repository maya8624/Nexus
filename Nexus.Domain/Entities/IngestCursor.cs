namespace Nexus.Domain.Entities
{
    public class IngestCursor
    {
        public string Source { get; set; } = default!;

        public DateTimeOffset LastPolledTo { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
