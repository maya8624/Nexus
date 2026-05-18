using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public class TenantPreferenceRequest
    {
        public string[] Suburbs { get; set; } = [];
        public int MaxRent { get; set; }
        public int MinBeds { get; set; }
        public int MaxBeds { get; set; }
        public bool PetFriendly { get; set; }
        public int AvailableWithinDays { get; set; }
    }

    public class TenantPreferenceRequestValidator : AbstractValidator<TenantPreferenceRequest>
    {
        public TenantPreferenceRequestValidator()
        {
            RuleFor(x => x.Suburbs)
                .NotEmpty();

            RuleFor(x => x.MaxRent)
                .GreaterThan(0);

            RuleFor(x => x.MinBeds)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.MaxBeds)
                .GreaterThanOrEqualTo(0)
                .GreaterThanOrEqualTo(x => x.MinBeds);

            RuleFor(x => x.AvailableWithinDays)
                .GreaterThan(0);
        }
    }
}