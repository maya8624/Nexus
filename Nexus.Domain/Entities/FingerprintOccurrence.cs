namespace Nexus.Domain.Entities
{
    public class FingerprintOccurrence
    {
        public long Id { get; set; }

        public string FingerprintId { get; set; } = default!;

        public DateTimeOffset OccurredAt { get; set; }

        public int OccurrenceCount { get; set; }

        public string? RenderedMessage { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public Fingerprint Fingerprint { get; set; } = default!;
    }
}
