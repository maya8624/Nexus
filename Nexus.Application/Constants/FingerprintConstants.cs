namespace Nexus.Application.Constants
{
    public static class FingerprintConstants
    {
        public const int SparklineHistoryHours = 7;
        public const int MinSpikeMultiplier = 3;
        public const int MinHoursBetweenComments = 1;
        public const int MinDigitRunLength = 3;
        public const string IngestCursorSource = "appinsights";
        public const int MaxSummarizeOccurrences = 5;
        public const int MaxDetailOccurrences = 10;
        public const string AutoFixCandidateLabel = "auto-fix-candidate";
    }
}
