using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class InspectionBookingRequest
    {
        public Guid InspectionSlotId { get; init; }
        public string? Notes { get; init; }
    }

    public sealed class InspectionBookingRequestValidator : AbstractValidator<InspectionBookingRequest>
    {
        public InspectionBookingRequestValidator()
        {
            RuleFor(x => x.InspectionSlotId).NotEmpty();
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }
}
