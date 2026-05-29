namespace Nexus.Domain.ValueObjects
{
    public class SourceChunk
    {
        public string FileName { get; init; } = string.Empty;
        public int? Page { get; init; }
        public float Score { get; init; }
        public string Text { get; init; } = string.Empty;
    }
}
