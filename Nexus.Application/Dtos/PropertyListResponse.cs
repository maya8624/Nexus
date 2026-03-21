using FluentValidation;

namespace Nexus.Application.Dtos
{
    public sealed class PropertyListResponse
    {
        public IReadOnlyList<PropertyDto> Items { get; init; } = Array.Empty<PropertyDto>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
    }

    public sealed class PropertyListResponseValidator : AbstractValidator<PropertyListResponse>
    {
        public PropertyListResponseValidator()
        {
            RuleFor(x => x.Items)
                .NotNull();

            RuleForEach(x => x.Items)
                .SetValidator(new PropertyDtoValidator());

            RuleFor(x => x.Page)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(100);

            RuleFor(x => x.TotalCount)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.TotalPages)
                .GreaterThanOrEqualTo(0);
        }
    }
}
