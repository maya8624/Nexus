using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class CreateInspectionBookingRequest
    {
        public Guid InspectionSlotId { get; init; }
        public string? Notes { get; init; }
    }

    public sealed class CreateInspectionBookingRequestValidator : AbstractValidator<CreateInspectionBookingRequest>
    {
        public CreateInspectionBookingRequestValidator()
        {
            RuleFor(x => x.InspectionSlotId).NotEmpty();
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }
}
