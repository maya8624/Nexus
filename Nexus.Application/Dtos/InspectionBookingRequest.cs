using FluentValidation;
using Nexus.Application.Dtos;
using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos
{
    public sealed class InspectionBookingRequest
    {
        public Guid PropertyId { get; init; }

        public Guid ListingId { get; init; }

        public Guid AgentId { get; init; }

        public InspectionBookingStatus Status { get; init; } = InspectionBookingStatus.Cancelled;

        public DateTimeOffset InspectionStartAtUtc { get; init; }

        public DateTimeOffset InspectionEndAtUtc { get; init; }

        public string? Notes { get; init; }
    }
}

public sealed class CreateInspectionBookingRequestValidator : AbstractValidator<InspectionBookingRequest>
{
    public CreateInspectionBookingRequestValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.ListingId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();

        RuleFor(x => x.InspectionStartAtUtc)
            .Must(x => x != default)
            .WithMessage("InspectionStartAtUtc is required.");

        RuleFor(x => x.InspectionEndAtUtc)
            .Must(x => x != default)
            .WithMessage("InspectionEndAtUtc is required.");

        RuleFor(x => x)
            .Must(x => x.InspectionStartAtUtc < x.InspectionEndAtUtc)
            .WithMessage("InspectionStartAtUtc must be earlier than InspectionEndAtUtc.");

        RuleFor(x => x.InspectionStartAtUtc)
            .Must(x => x > DateTimeOffset.UtcNow)
            .WithMessage("InspectionStartAtUtc must be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
