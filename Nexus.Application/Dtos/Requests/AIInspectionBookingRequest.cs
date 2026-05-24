using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class AiInspectionBookingRequest
    {
        public Guid UserId { get; set; }
    }

    public sealed class AiInspectionBookingRequestValidator : AbstractValidator<AiInspectionBookingRequest>
    {
        public AiInspectionBookingRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}