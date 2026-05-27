using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class EnquiryDraftRequest
    {
        public Guid Id { get; init; }
    }

    public sealed class EnquiryDraftRequestValidator : AbstractValidator<EnquiryDraftRequest>
    {
        public EnquiryDraftRequestValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
