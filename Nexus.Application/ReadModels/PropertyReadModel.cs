using Nexus.Domain.Enums;

namespace Nexus.Application.ReadModels
{
    public sealed class PropertyReadModel
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? AddressLine1 { get; init; }
        public string? AddressLine2 { get; init; }
        public string Suburb { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string Postcode { get; init; } = string.Empty;
        public decimal PriceValue { get; init; }
        public ListingType? ListingType { get; init; }
        public string PropertyType { get; init; } = string.Empty;
        public int Bedrooms { get; init; }
        public int Bathrooms { get; init; }
        public int Parking { get; init; }
        public decimal? LandSizeSqm { get; init; }
        public string Description { get; init; } = string.Empty;
        public IReadOnlyList<string> Images { get; init; } = Array.Empty<string>();
        public string AgentFirstName { get; init; } = string.Empty;
        public string AgentLastName { get; init; } = string.Empty;
        public string AgentPhone { get; init; } = string.Empty;
        public string AgentPhoto { get; init; } = string.Empty;
        public string AgencyName { get; init; } = string.Empty;
        public DateTimeOffset? ListedAtUtc { get; init; }
    }
}
