using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Nexus.Application.Constants;

namespace Nexus.Application.Common
{
    public static class FingerprintHasher
    {
        private static readonly Regex DoubleQuotedRegex = new(@"""[^""]*""", RegexOptions.Compiled);
        private static readonly Regex SingleQuotedRegex = new(@"'[^']*'", RegexOptions.Compiled);

        private static readonly Regex GuidRegex = new(
            @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
            RegexOptions.Compiled);

        private static readonly Regex DigitRunRegex = new(
            $@"\b\d{{{FingerprintConstants.MinDigitRunLength},}}\b",
            RegexOptions.Compiled);

        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Computes the fingerprint hash for an App Insights exception, namespaced so it can never collide with a trace hash of the same raw value.
        /// </summary>
        /// <param name="problemId">App Insights' own "same exception shape" grouping key.</param>
        /// <returns>Lowercase 64-character SHA-256 hex digest.</returns>
        public static string ComputeExceptionHash(string problemId)
            => ComputeHash("appinsights|exception|" + problemId);

        /// <summary>
        /// Computes the fingerprint hash for an App Insights trace warning, namespaced so it can never collide with an exception hash of the same raw value.
        /// </summary>
        /// <param name="normalizedMessage">A message already passed through <see cref="NormalizeMessage"/>.</param>
        /// <returns>Lowercase 64-character SHA-256 hex digest.</returns>
        public static string ComputeTraceHash(string normalizedMessage)
            => ComputeHash("appinsights|trace|" + normalizedMessage);

        /// <summary>
        /// Derives the short fingerprint id used as <c>Fingerprint.Id</c> from a full hash digest.
        /// </summary>
        /// <param name="hashHex">Hex digest produced by <see cref="ComputeExceptionHash"/> or <see cref="ComputeTraceHash"/>.</param>
        /// <returns><c>"fp_"</c> followed by the first 8 hex characters of <paramref name="hashHex"/>.</returns>
        public static string GenerateFingerprintId(string hashHex)
            => "fp_" + hashHex[..8];

        /// <summary>
        /// Strips variable data (quoted values, GUIDs, digit runs of 3+) from a raw trace message so occurrences
        /// that differ only in those values normalize to the same string, and therefore hash to the same fingerprint.
        /// </summary>
        /// <param name="raw">The raw trace message text.</param>
        /// <returns>The normalized message, with variable substrings replaced by placeholder tokens and whitespace collapsed.</returns>
        public static string NormalizeMessage(string raw)
        {
            var normalized = DoubleQuotedRegex.Replace(raw, "{str}");
            normalized = SingleQuotedRegex.Replace(normalized, "{str}");
            normalized = GuidRegex.Replace(normalized, "{guid}");
            normalized = DigitRunRegex.Replace(normalized, "{n}");
            normalized = WhitespaceRegex.Replace(normalized, " ");

            return normalized.Trim();
        }

        /// <summary>
        /// Computes a SHA-256 hash of the given input, shared by both public hash methods so they only differ by their namespacing prefix.
        /// </summary>
        /// <param name="input">The already-prefixed string to hash.</param>
        /// <returns>Lowercase 64-character hex digest.</returns>
        private static string ComputeHash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
