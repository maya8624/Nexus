using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class GetUploadUrlRequest
    {
        public string FileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
    }

    public sealed class GetUploadUrlRequestValidator : AbstractValidator<GetUploadUrlRequest>
    {
        private static readonly HashSet<string> _allowedTypes =
        [
            "image/jpeg", "image/png", "image/webp",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];

        public GetUploadUrlRequestValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .Must(t => _allowedTypes.Contains(t))
                .WithMessage("Unsupported file type.");
        }
    }
}
