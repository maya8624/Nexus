namespace Nexus.Application.Settings
{
    public class RateLimitSettings
    {
        public RateLimitPolicySettings Login { get; init; } = new();
        public RateLimitPolicySettings Register { get; init; } = new();
        public RateLimitPolicySettings Refresh { get; init; } = new();
    }

    public class RateLimitPolicySettings
    {
        public int PermitLimit { get; init; }
        public int WindowMinutes { get; init; }
        public int SegmentsPerWindow { get; init; } = 6;
    }
}
