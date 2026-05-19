namespace Nexus.Application.Dtos.Responses
{
    public class PreferenceSearchResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<ListingItem> Listings { get; set; } = [];
        public int DisplayCount { get; set; }
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }

    public class ListingItem
    {
        public string PropertyId { get; set; } = string.Empty;
        public string ListingId { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string Suburb { get; set; } = string.Empty;
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public decimal Price { get; set; }
        public decimal? BuildingSizeSqm { get; set; }
        public string PropertyType { get; set; } = string.Empty;
    }
}
