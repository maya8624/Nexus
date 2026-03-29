using FluentValidation;

namespace Nexus.Application.Dtos
{
    public sealed class PropertyDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string Suburb { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string Postcode { get; init; } = string.Empty;
        public string Price { get; init; } = string.Empty;
        public decimal PriceValue { get; init; }
        public string PropertyType { get; init; } = string.Empty;
        public int Bedrooms { get; init; }
        public int Bathrooms { get; init; }
        public int Parking { get; init; }
        public int LandSize { get; init; }
        public string Description { get; init; } = string.Empty;
        public string[] Features { get; init; } = Array.Empty<string>();
        public string[] Images { get; init; } = Array.Empty<string>();
        public AgentDto Agent { get; init; } = new();
        public string? AuctionDate { get; init; }
        public bool IsNew { get; init; }
        public bool IsFeatured { get; init; }
        public string[] InspectionTimes { get; init; } = Array.Empty<string>();
        public string ListedDate { get; init; } = string.Empty;
    }
}
