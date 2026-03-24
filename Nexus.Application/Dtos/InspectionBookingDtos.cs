using FluentValidation;

namespace Nexus.Application.Dtos
{
    public sealed class CreateInspectionBookingRequest
    {
        public Guid UserId { get; init; }

        public Guid PropertyId { get; init; }

        public Guid ListingId { get; init; }

        public Guid? AgentId { get; init; }

        public DateTimeOffset InspectionStartAtUtc { get; init; }

        public DateTimeOffset InspectionEndAtUtc { get; init; }

        public string? Notes { get; init; }
    }

    public sealed class CheckInspectionAvailabilityRequest
    {
        public Guid PropertyId { get; init; }

        public Guid ListingId { get; init; }

        public DateTimeOffset InspectionStartAtUtc { get; init; }

        public DateTimeOffset InspectionEndAtUtc { get; init; }

        public Guid? ExcludeBookingId { get; init; }
    }

    public sealed class InspectionBookingDto
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        public Guid PropertyId { get; init; }

        public Guid ListingId { get; init; }

        public Guid? AgentId { get; init; }

        public DateTimeOffset InspectionStartAtUtc { get; init; }

        public DateTimeOffset InspectionEndAtUtc { get; init; }

        public string Status { get; init; } = string.Empty;

        public string? Notes { get; init; }

        public DateTimeOffset CreatedAtUtc { get; init; }

        public DateTimeOffset UpdatedAtUtc { get; init; }
    }

    public sealed class InspectionAvailabilityResponse
    {
        public bool IsAvailable { get; init; }

        public string Message { get; init; } = string.Empty;
    }

    public sealed class CreateInspectionBookingRequestValidator : AbstractValidator<CreateInspectionBookingRequest>
    {
        public CreateInspectionBookingRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
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

            RuleFor(x => x.InspectionStartAtUtc)
                .Must(x => x > DateTimeOffset.UtcNow)
                .WithMessage("InspectionStartAtUtc must be in the future.");

            RuleFor(x => x.Notes)
                .MaximumLength(1000);
        }
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
