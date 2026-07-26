namespace Nexus.Application.Common
{
    public static class FingerprintFilingPolicy
    {
        public const int MinOccurrencesToFile = 3;

        public static bool ShouldFileIssue(int windowOccurrenceCount, bool isNewRegression)
            => isNewRegression || windowOccurrenceCount >= MinOccurrencesToFile;
    }
}
