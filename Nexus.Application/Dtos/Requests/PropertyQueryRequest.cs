using FluentValidation;
using Nexus.Domain.Enums;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class PropertyQueryRequest : PaginationRequest
    {
        public string? Type { get; init; }
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

            RuleFor(x => x.Type)
                .Must(BeAValidPropertyType)
                .When(x => string.IsNullOrWhiteSpace(x.Type) == false)
                .WithMessage("Type must be one of: House, Apartment, Townhouse, Villa, Land.");
        }

        private static bool BeAValidPropertyType(string? type)
        {
            return Enum.TryParse<PropertyType>(type, ignoreCase: true, out _);
        }
    }
}
