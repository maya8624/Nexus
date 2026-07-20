namespace Nexus.Domain.Enums
{
    public enum FingerprintLevel
    {
        Error = 1,
        Warning = 2
    }

    public enum FingerprintCategory
    {
        DependencyFailure = 1,
        NewRegression = 2,
        RecurringKnown = 3,
        ConfigAuth = 4,
        DataQuality = 5,
        Performance = 6
    }

    public enum ClassificationSource
    {
        Rule = 1,
        Llm = 2
    }

    public enum GithubIssueStatus
    {
        None = 1,
        Open = 2,
        Closed = 3,
        Pr = 4,
        Merged = 5
    }
}
