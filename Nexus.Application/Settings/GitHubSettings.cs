namespace Nexus.Application.Settings
{
    public class GitHubSettings
    {
        public required string Token { get; init; }
        public required string Owner { get; init; }
        public required string Repo { get; init; }
    }
}
