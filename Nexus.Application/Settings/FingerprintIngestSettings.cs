namespace Nexus.Application.Settings
{
    public class FingerprintIngestSettings
    {
        public required string WorkspaceId { get; init; }
        public int InitialLookbackMinutes { get; init; } = 60;
        public int IngestionSafetyLagMinutes { get; init; } = 5;

        /// <summary>
        /// Logger category prefixes whose traces are eligible for fingerprinting. Traces carry no
        /// ProblemId, so third-party libraries that log exception text as a plain string (MSAL via
        /// Azure.Identity is the observed case) would otherwise create a second fingerprint for a
        /// failure that already has one from AppExceptions. Restricting to first-party categories
        /// also keeps framework chatter — Hangfire shutdown notices, HTTPS-redirect warnings — out
        /// of the triage queue. An empty array disables the filter and ingests every category.
        /// </summary>
        public string[] TraceCategoryPrefixes { get; init; } = ["Nexus."];
    }
}
