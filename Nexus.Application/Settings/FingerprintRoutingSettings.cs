namespace Nexus.Application.Settings
{
    public class FingerprintRoutingSettings
    {
        public List<OwnershipRule> Ownership { get; init; } = [];
        public required string DefaultAssignee { get; init; }
        public List<string> AutoFixAllowlistCategories { get; init; } = [];
        public List<string> AutoFixDenylistNamespaces { get; init; } = [];
    }

    public class OwnershipRule
    {
        public OwnershipMatch Match { get; init; } = new();
        public required string Assignee { get; init; }
    }

    public class OwnershipMatch
    {
        public string? ServiceName { get; init; }
        public string? OperationPrefix { get; init; }
        public string? Category { get; init; }
    }
}
