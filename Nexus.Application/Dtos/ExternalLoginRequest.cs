using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Application.Dtos
{
    public class ExternalLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty; // e.g., "google"
    }

    public class ExternalLoginRequestValidator : AbstractValidator<ExternalLoginRequest>
    {
        public ExternalLoginRequestValidator()
        {
            RuleFor(x => x.IdToken).NotEmpty();
            RuleFor(x => x.Provider).NotEmpty();
        }
    }
}
