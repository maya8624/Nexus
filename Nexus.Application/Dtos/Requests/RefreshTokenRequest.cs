using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = default!;
    }

    public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
    {
        public RefreshTokenRequestValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}
