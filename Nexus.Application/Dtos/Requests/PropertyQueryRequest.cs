using FluentValidation;
using Nexus.Domain.Enums;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class PropertyQueryRequest : PaginationRequest
    {
        public string? PropertyType { get; init; }
        public string? ListingType { get; init; }
    }

    public sealed class PropertyQueryRequestValidator : AbstractValidator<PropertyQueryRequest>
    {
        public PropertyQueryRequestValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(100);

            RuleFor(x => x.PropertyType)
                .Must(BeAValidPropertyType)
                .When(x => string.IsNullOrWhiteSpace(x.PropertyType) == false)
                .WithMessage("Type must be one of: House, Apartment, Townhouse, Villa, Land.");

            RuleFor(x => x.ListingType)
                .Must(BeAValidListingType)
                .When(x => string.IsNullOrWhiteSpace(x.ListingType) == false)
                .WithMessage("ListingType must be one of: Sale, Rent.");
        }

        private static bool BeAValidPropertyType(string? type)
        {
            return Enum.TryParse<PropertyType>(type, ignoreCase: true, out _);
        }

        private static bool BeAValidListingType(string? type)
        {
            return Enum.TryParse<ListingType>(type, ignoreCase: true, out _);
        }
    }
}
