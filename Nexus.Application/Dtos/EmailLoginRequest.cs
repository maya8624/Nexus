using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Application.Dtos
{
    public class EmailLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class EmailLoginRequestValidator : AbstractValidator<EmailLoginRequest>
    {
        public EmailLoginRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}
