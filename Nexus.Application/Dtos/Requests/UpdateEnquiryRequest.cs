using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class UpdateEnquiryRequest
    {
        public string Body { get; init; } = string.Empty;
        public string DraftReply { get; init; } = string.Empty;
        public string SentReply { get; init; } = string.Empty;
    }

    public sealed class UpdateEnquiryRequestValidator : AbstractValidator<UpdateEnquiryRequest>
    {
        public UpdateEnquiryRequestValidator()
        {
            RuleFor(x => x.Body).NotEmpty().MaximumLength(1000);
        }
    }
}
