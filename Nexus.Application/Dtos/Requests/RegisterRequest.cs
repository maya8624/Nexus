using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(6);

            RuleFor(x => x.FirstName)
                .MaximumLength(100)
                .When(x => x.FirstName != null);

            RuleFor(x => x.LastName)
                .MaximumLength(100)
                .When(x => x.LastName != null);
        }
    }
}
