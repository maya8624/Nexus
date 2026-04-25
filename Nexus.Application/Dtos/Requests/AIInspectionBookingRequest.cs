using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class AIInspectionBookingRequest
    {
        public Guid UserId { get; set; }
    }

    public sealed class AIInspectionBookingRequestValidator : AbstractValidator<AIInspectionBookingRequest>
    {
        public AIInspectionBookingRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}