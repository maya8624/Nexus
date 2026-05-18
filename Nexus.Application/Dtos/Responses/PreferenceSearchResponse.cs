using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nexus.Application.Dtos.Responses
{
    public class PreferenceSearchResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;

        [JsonPropertyName("listings")]
        public List<Dictionary<string, object>> Listings { get; set; } = [];

        [JsonPropertyName("display_count")]
        public int DisplayCount { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }
}
