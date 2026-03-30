using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class CheckInspectionAvailabilityRequest
    {
        public Guid PropertyId { get; init; }

        public Guid ListingId { get; init; }

        public DateTimeOffset InspectionStartAtUtc { get; init; }

        public DateTimeOffset InspectionEndAtUtc { get; init; }

        public Guid? ExcludeBookingId { get; init; }
    }

    public sealed class CheckInspectionAvailabilityRequestValidator : AbstractValidator<CheckInspectionAvailabilityRequest>
    {
        public CheckInspectionAvailabilityRequestValidator()
        {
            RuleFor(x => x.PropertyId).NotEmpty();
            RuleFor(x => x.ListingId).NotEmpty();

            RuleFor(x => x.InspectionStartAtUtc)
                .Must(x => x != default)
                .WithMessage("InspectionStartAtUtc is required.");

            RuleFor(x => x.InspectionEndAtUtc)
                .Must(x => x != default)
                .WithMessage("InspectionEndAtUtc is required.");

            RuleFor(x => x)
                .Must(x => x.InspectionStartAtUtc < x.InspectionEndAtUtc)
                .WithMessage("InspectionStartAtUtc must be earlier than InspectionEndAtUtc.");
        }
    }
}
