using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusPay.Application.Dtos
{
    public class LocalLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LocalLoginRequestValidator : AbstractValidator<LocalLoginRequest>
    {
        public LocalLoginRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}
