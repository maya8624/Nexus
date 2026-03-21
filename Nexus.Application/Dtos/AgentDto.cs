using FluentValidation;

namespace Nexus.Application.Dtos
{
    public sealed class AgentDto
    {
        public string Name { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Agency { get; init; } = string.Empty;
        public string Photo { get; init; } = string.Empty;
    }

    public sealed class AgentDtoValidator : AbstractValidator<AgentDto>
    {
        public AgentDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Phone)
                .MaximumLength(50);

            RuleFor(x => x.Agency)
                .MaximumLength(200);

            RuleFor(x => x.Photo)
                .MaximumLength(1000);
        }
    }
}
