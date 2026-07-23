using Nexus.Application.Common;
using Nexus.Application.Constants;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class FingerprintHasherTests
    {
        [Fact]
        public void ComputeExceptionHash_SameProblemId_ProducesSameHash()
        {
            var hash1 = FingerprintHasher.ComputeExceptionHash("problem-123");
            var hash2 = FingerprintHasher.ComputeExceptionHash("problem-123");

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void ComputeExceptionHash_DifferentProblemId_ProducesDifferentHash()
        {
            var hash1 = FingerprintHasher.ComputeExceptionHash("problem-123");
            var hash2 = FingerprintHasher.ComputeExceptionHash("problem-456");

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void ComputeTraceHash_SameNormalizedMessage_ProducesSameHash()
        {
            var hash1 = FingerprintHasher.ComputeTraceHash("Connection refused to {n}.{n}.{n}.{n}:{n}");
            var hash2 = FingerprintHasher.ComputeTraceHash("Connection refused to {n}.{n}.{n}.{n}:{n}");

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void ComputeTraceHash_DifferentNormalizedMessage_ProducesDifferentHash()
        {
            var hash1 = FingerprintHasher.ComputeTraceHash("Connection refused to {n}.{n}.{n}.{n}:{n}");
            var hash2 = FingerprintHasher.ComputeTraceHash("Timeout waiting for {n}.{n}.{n}.{n}:{n}");

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void ComputeExceptionHash_And_ComputeTraceHash_SameRawInput_ProduceDifferentHashes()
        {
            var exceptionHash = FingerprintHasher.ComputeExceptionHash("same-value");
            var traceHash = FingerprintHasher.ComputeTraceHash("same-value");

            Assert.NotEqual(exceptionHash, traceHash);
        }

        [Fact]
        public void ComputeExceptionHash_ReturnsLowercase64CharHex()
        {
            // Fingerprint.Hash is HasMaxLength(64) in the EF config, so the exact
            // format (not just determinism) matters here.
            var hash = FingerprintHasher.ComputeExceptionHash("problem-123");

            Assert.Equal(64, hash.Length);
            Assert.Matches("^[0-9a-f]{64}$", hash);
        }

        [Fact]
        public void GenerateFingerprintId_HashHex_ReturnsFpPrefixedFirst8Chars()
        {
            var hash = FingerprintHasher.ComputeExceptionHash("problem-123");

            var id = FingerprintHasher.GenerateFingerprintId(hash);

            Assert.StartsWith("fp_", id);
            Assert.Equal("fp_" + hash[..8], id);
            Assert.Equal(11, id.Length);
        }

        [Fact]
        public void NormalizeMessage_DoubleQuotedString_ReplacedWithPlaceholder()
        {
            var result = FingerprintHasher.NormalizeMessage("Failed to parse \"user-input-value\" from request");

            Assert.Equal("Failed to parse {str} from request", result);
        }

        [Fact]
        public void NormalizeMessage_SingleQuotedString_ReplacedWithPlaceholder()
        {
            var result = FingerprintHasher.NormalizeMessage("Failed to parse 'user-input-value' from request");

            Assert.Equal("Failed to parse {str} from request", result);
        }

        [Fact]
        public void NormalizeMessage_Guid_ReplacedWithPlaceholder()
        {
            var result = FingerprintHasher.NormalizeMessage("Entity a1b2c3d4-e5f6-7890-abcd-ef1234567890 not found");

            Assert.Equal("Entity {guid} not found", result);
        }

        [Fact]
        public void NormalizeMessage_DigitRunAtOrAboveThreshold_ReplacedWithPlaceholder()
        {
            // Only runs >= MinDigitRunLength (3) are stripped, so the 1-2 digit
            // octets in the IP are left alone — only the 4-digit port collapses.
            var result = FingerprintHasher.NormalizeMessage("Connection refused to 10.0.0.4:5432");

            Assert.Equal("Connection refused to 10.0.0.4:{n}", result);
        }

        [Fact]
        public void NormalizeMessage_DigitRunBelowThreshold_LeftUnchanged()
        {
            var result = FingerprintHasher.NormalizeMessage("Retry attempt 12 failed");

            Assert.Equal("Retry attempt 12 failed", result);
        }

        [Fact]
        public void NormalizeMessage_DigitRunAtExactThreshold_ReplacedWithPlaceholder()
        {
            var digitRun = new string('9', FingerprintConstants.MinDigitRunLength);

            var result = FingerprintHasher.NormalizeMessage($"Value was {digitRun} exactly");

            Assert.Equal("Value was {n} exactly", result);
        }

        [Fact]
        public void NormalizeMessage_DigitsEmbeddedInIdentifier_LeftUnchanged()
        {
            // No word boundary between letters and digits in the same token, so an
            // identifier like "user123" is never treated as a bare number to strip.
            var result = FingerprintHasher.NormalizeMessage("Failed to load user123 from cache");

            Assert.Equal("Failed to load user123 from cache", result);
        }

        [Fact]
        public void NormalizeMessage_QuotedGuidLikeSubstring_NotTouchedByGuidPass()
        {
            // Quoted-string stripping runs before the GUID pass, so a GUID-shaped
            // literal inside quotes should collapse to {str}, not {guid}.
            var result = FingerprintHasher.NormalizeMessage("Header \"a1b2c3d4-e5f6-7890-abcd-ef1234567890\" was invalid");

            Assert.Equal("Header {str} was invalid", result);
        }

        [Fact]
        public void NormalizeMessage_RepeatedWhitespace_CollapsedAndTrimmed()
        {
            var result = FingerprintHasher.NormalizeMessage("  Too    many    spaces  \t here  ");

            Assert.Equal("Too many spaces here", result);
        }

        [Fact]
        public void NormalizeMessage_MixedCase_PreservesCase()
        {
            var result = FingerprintHasher.NormalizeMessage("Connection Refused To RagService");

            Assert.Equal("Connection Refused To RagService", result);
        }
    }
}
