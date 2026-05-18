namespace Nexus.Application.Dtos.Requests
{
    /// <summary>
    /// Internal DTO used by AiService to communicate with Python service.
    /// Matches the Python TenantPreference schema exactly.
    /// </summary>
    public class TenantPreferenceAiRequest
    {
        // snake_case to match Python schema
        public required string[] suburbs { get; init; }
        public decimal? maxRent { get; init; }
        public int? minBeds { get; init; }
        public int? maxBeds { get; init; }
        public bool petFriendly { get; init; }
        public int? availableWithinDays { get; init; }
    }
}
