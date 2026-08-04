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

        /// <summary>
        /// How far back the retry pass looks for fingerprints still sitting at
        /// <see cref="Nexus.Domain.Enums.GithubIssueStatus.None"/>. Zero or less disables the retry.
        /// </summary>
        /// <remarks>
        /// A fingerprint reaches None-after-creation only when its filing failed — the poll died before
        /// the actor ran, GitHub errored, or no token was configured — because the filing policy always
        /// approves a brand-new fingerprint. The cursor has already advanced past its window, so nothing
        /// re-examines it unless the same error recurs. The bound matters: without it the job would keep
        /// retrying seeded mock rows and every failure ever accumulated against a blank GitHub token.
        /// </remarks>
        public int MissedIssueLookbackHours { get; init; } = 24;

        /// <summary>
        /// Maximum fingerprints the retry pass will hand to the GitHub actor in a single run, so a large
        /// backlog can't turn one poll into hundreds of GitHub and AI calls. The remainder is picked up
        /// by later runs while it stays inside the lookback window.
        /// </summary>
        public int MaxMissedIssueRetriesPerRun { get; init; } = 25;
    }
}
