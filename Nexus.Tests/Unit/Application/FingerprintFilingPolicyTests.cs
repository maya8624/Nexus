using Nexus.Application.Common;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class FingerprintFilingPolicyTests
    {
        [Fact]
        public void ShouldFileIssue_WhenIsNewRegression_ReturnsTrueRegardlessOfCount()
        {
            Assert.True(FingerprintFilingPolicy.ShouldFileIssue(windowOccurrenceCount: 1, isNewRegression: true));
        }

        [Fact]
        public void ShouldFileIssue_WhenCountMeetsThreshold_ReturnsTrue()
        {
            Assert.True(FingerprintFilingPolicy.ShouldFileIssue(
                windowOccurrenceCount: FingerprintFilingPolicy.MinOccurrencesToFile, isNewRegression: false));
        }

        [Fact]
        public void ShouldFileIssue_WhenCountBelowThresholdAndNotNew_ReturnsFalse()
        {
            Assert.False(FingerprintFilingPolicy.ShouldFileIssue(
                windowOccurrenceCount: FingerprintFilingPolicy.MinOccurrencesToFile - 1, isNewRegression: false));
        }
    }
}
