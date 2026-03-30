namespace Nexus.Application.Dtos.Responses
{
    public sealed class ErrorResponse
    {
        public int Code { get; init; }
        public string Name { get; init; } = default!;
        public string Message { get; init; } = default!;
    }
}
