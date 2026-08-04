namespace Nexus.Application.Common
{
    /// <summary>
    /// Builds the browser URL for a fingerprint's filed GitHub issue.
    /// </summary>
    /// <remarks>
    /// Derived rather than persisted. <c>Fingerprint</c> stores only the issue number, and GitHub
    /// redirects issue URLs after a repository rename, so a derived URL stays correct where a stored
    /// one would go stale. The tradeoff is that this assumes every issue lives in the currently
    /// configured repository — if <c>GitHubSettings.Owner</c>/<c>Repo</c> is ever repointed, links on
    /// historical fingerprints follow it.
    /// </remarks>
    public static class GitHubIssueUrlBuilder
    {
        /// <summary>
        /// Returns the issue URL, or <c>null</c> when there is no issue or no configured repository.
        /// </summary>
        /// <remarks>
        /// The owner/repo guard matters locally: <c>GitHubSettings</c> is blank when no token is
        /// configured, and without it a seeded fingerprint would advertise
        /// <c>https://github.com//issues/123</c> as a real link.
        /// </remarks>
        public static string? Build(string? owner, string? repo, int? issueNumber)
        {
            if (issueNumber is null || string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
                return null;

            return $"https://github.com/{owner}/{repo}/issues/{issueNumber.Value}";
        }
    }
}
