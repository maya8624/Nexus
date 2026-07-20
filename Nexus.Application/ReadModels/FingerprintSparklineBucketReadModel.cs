namespace Nexus.Application.ReadModels
{
    public sealed class FingerprintSparklineBucketReadModel
    {
        public DateTimeOffset BucketStart { get; init; }
        public int Count { get; init; }
    }
}
