using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public class SuburbSummaryRequest
    {
        public string[] Suburbs { get; set; } = [];
    }

    public class SuburbSummaryRequestValidator : AbstractValidator<SuburbSummaryRequest>
    {
        public SuburbSummaryRequestValidator()
        {
            RuleFor(x => x.Suburbs)
                .NotEmpty();
        }
    }
}
