using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class ConfirmUploadRequest
    {
        public long? FileSizeBytes { get; init; }
    }

    public sealed class ConfirmUploadRequestValidator : AbstractValidator<ConfirmUploadRequest>
    {
        public ConfirmUploadRequestValidator()
        {
            RuleFor(x => x.FileSizeBytes)
                .GreaterThan(0)
                .When(x => x.FileSizeBytes.HasValue)
                .WithMessage("FileSizeBytes must be greater than 0.");
        }
    }
}
