using Nexus.Application.Dtos;

namespace Nexus.Application.Dtos.Responses
{
    public sealed class PropertyListResponse
    {
        public IReadOnlyList<PropertyDto> Items { get; init; } = [];
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
    }
}
