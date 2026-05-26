using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class CreateEnquiryRequest
    {
        public Guid PropertyId { get; init; }
        public Guid? ListingId { get; init; }
        public Guid AgentId { get; init; }
        public string Body { get; init; } = string.Empty;
    }

    public sealed class CreateEnquiryRequestValidator : AbstractValidator<CreateEnquiryRequest>
    {
        public CreateEnquiryRequestValidator()
        {
            RuleFor(x => x.PropertyId).NotEmpty();
            RuleFor(x => x.AgentId).NotEmpty();
            RuleFor(x => x.Body).NotEmpty().MaximumLength(1000);
        }
    }
}
