using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class InspectionSlotRequest
    {
        public Guid PropertyId { get; init; }
        public Guid? ListingId { get; init; }
        public Guid AgentId { get; init; }
        public DateTimeOffset StartAtUtc { get; init; }
        public DateTimeOffset EndAtUtc { get; init; }
        public int Capacity { get; init; }
        public string? Notes { get; init; }
    }

    public sealed class InspectionSlotRequestValidator : AbstractValidator<InspectionSlotRequest>
    {
        public InspectionSlotRequestValidator()
        {
            RuleFor(x => x.PropertyId)
                .NotEmpty();

            RuleFor(x => x.AgentId)
                .NotEmpty();

            RuleFor(x => x.StartAtUtc)
                .NotEmpty()
                .GreaterThan(DateTimeOffset.UtcNow)
                .WithMessage("StartAtUtc must be in the future.");

            RuleFor(x => x.EndAtUtc)
                .NotEmpty()
                .GreaterThan(x => x.StartAtUtc)
                .WithMessage("EndAtUtc must be later than StartAtUtc.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0);

            RuleFor(x => x.Notes)
                .MaximumLength(1000);
        }
    }
}