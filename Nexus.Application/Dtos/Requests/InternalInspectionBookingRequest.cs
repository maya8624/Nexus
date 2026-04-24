using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class InternalInspectionBookingRequest
    {
        public Guid InspectionSlotId { get; init; }
        public Guid UserId { get; init; }
        public string? Notes { get; init; }
    }

    public sealed class InternalInspectionBookingRequestValidator : AbstractValidator<InternalInspectionBookingRequest>
    {
        public InternalInspectionBookingRequestValidator()
        {
            RuleFor(x => x.InspectionSlotId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }
}