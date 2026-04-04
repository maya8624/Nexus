using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public class CreateDepositRequest
    {
        public Guid PropertyId { get; set; }
        public Guid ListingId { get; set; }
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public class CreateDepositRequestValidator : AbstractValidator<CreateDepositRequest>
    {
        public CreateDepositRequestValidator()
        {
            RuleFor(x => x.PropertyId).NotEmpty();
            RuleFor(x => x.ListingId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(100);
        }
    }
}
