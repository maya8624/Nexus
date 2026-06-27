using FluentValidation;
using Nexus.Domain.Enums;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class GetUploadUrlRequest
    {
        public string FileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public UploadPurpose Purpose { get; init; }
    }

    public sealed class GetUploadUrlRequestValidator : AbstractValidator<GetUploadUrlRequest>
    {
        private static readonly HashSet<string> _allAllowedTypes =
        [
            "image/jpeg", "image/png", "image/webp",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];

        private static readonly HashSet<string> _extractionTypes =
        [
            "image/jpeg", "image/png", "image/webp"
        ];

        private static readonly HashSet<string> _ingestionTypes =
        [
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
                .Must(t => _allAllowedTypes.Contains(t))
                .WithMessage("Unsupported file type.");

            RuleFor(x => x.Purpose)
                .IsInEnum()
                .WithMessage("Invalid upload purpose.");

            RuleFor(x => x)
                .Must(r => !_extractionTypes.Contains(r.ContentType) || r.Purpose == UploadPurpose.Extraction || r.Purpose == UploadPurpose.Invoice || r.Purpose == UploadPurpose.General)
                .WithMessage("Image files can only be used with General, Extraction, or Invoice purpose.")
                .Must(r => !_ingestionTypes.Contains(r.ContentType) || r.Purpose == UploadPurpose.Ingestion || r.Purpose == UploadPurpose.Invoice || r.Purpose == UploadPurpose.General)
                .WithMessage("Document files can only be used with General, Ingestion, or Invoice purpose.")
                .Must(r => r.Purpose != UploadPurpose.Extraction || _extractionTypes.Contains(r.ContentType))
                .WithMessage("Extraction purpose only supports image files (jpeg, png, webp).")
                .Must(r => r.Purpose != UploadPurpose.Ingestion || _ingestionTypes.Contains(r.ContentType))
                .WithMessage("Ingestion purpose only supports document files (pdf, doc, docx).");
        }
    }
}
