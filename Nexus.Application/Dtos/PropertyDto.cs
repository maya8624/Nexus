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

    public sealed class PropertyDtoValidator : AbstractValidator<PropertyDto>
    {
        private static readonly string[] ValidPropertyTypes =
        [
            "House",
            "Apartment",
            "Townhouse",
            "Villa",
            "Land"
        ];

        public PropertyDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(300);

            RuleFor(x => x.Address)
                .NotEmpty()
                .MaximumLength(300);

            RuleFor(x => x.Suburb)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.State)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Postcode)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.Price)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.PriceValue)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.PropertyType)
                .NotEmpty()
                .Must(x => ValidPropertyTypes.Contains(x))
                .WithMessage("PropertyType must be one of: House, Apartment, Townhouse, Villa, Land.");

            RuleFor(x => x.Bedrooms)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Bathrooms)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Parking)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.LandSize)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Description)
                .MaximumLength(4000);

            RuleFor(x => x.Features)
                .NotNull();

            RuleForEach(x => x.Features)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Images)
                .NotNull();

            RuleForEach(x => x.Images)
                .NotEmpty()
                .MaximumLength(1000);

            RuleFor(x => x.Agent)
                .NotNull()
                .SetValidator(new AgentDtoValidator());

            RuleForEach(x => x.InspectionTimes)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.ListedDate)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.AuctionDate)
                .MaximumLength(100);
        }
    }
}
