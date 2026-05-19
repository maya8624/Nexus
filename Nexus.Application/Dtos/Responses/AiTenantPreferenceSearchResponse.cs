using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nexus.Application.Dtos.Responses
{
    public class AiTenantPreferenceSearchResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;

        [JsonPropertyName("listings")]
        public List<AiListingResult> Listings { get; set; } = [];

        [JsonPropertyName("display_count")]
        public int DisplayCount { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }

    public class AiListingResult
    {
        [JsonPropertyName("property_id")]
        public string PropertyId { get; set; } = string.Empty;

        [JsonPropertyName("listing_id")]
        public string ListingId { get; set; } = string.Empty;

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("address_line1")]
        public string AddressLine1 { get; set; } = string.Empty;

        [JsonPropertyName("suburb")]
        public string Suburb { get; set; } = string.Empty;

        [JsonPropertyName("bedrooms")]
        public int Bedrooms { get; set; }

        [JsonPropertyName("bathrooms")]
        public int Bathrooms { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("building_size_sqm")]
        public decimal? BuildingSizeSqm { get; set; }

        [JsonPropertyName("property_type")]
        public string PropertyType { get; set; } = string.Empty;
    }
}
