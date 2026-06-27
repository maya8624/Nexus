using Nexus.Domain.Enums;

namespace Nexus.Application.Dtos.Responses
{
    public sealed class InvoiceLineItemResponse
    {
        public string? Description { get; init; }
        public decimal? Quantity { get; init; }
        public decimal? UnitPrice { get; init; }
        public decimal? Amount { get; init; }
    }

    public sealed class InvoiceResponse
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public Guid? FileUploadId { get; init; }
        public DocumentType DocumentType { get; init; }
        public string? Filename { get; init; }
        public string? VendorName { get; init; }
        public string? VendorAddress { get; init; }
        public string? CustomerName { get; init; }
        public string? InvoiceNumber { get; init; }
        public DateOnly? InvoiceDate { get; init; }
        public DateOnly? DueDate { get; init; }
        public decimal? Subtotal { get; init; }
        public decimal? Tax { get; init; }
        public decimal? Total { get; init; }
        public string? Currency { get; init; }
        public double Confidence { get; init; }
        public List<InvoiceLineItemResponse> LineItems { get; init; } = [];
        public DateTimeOffset CreatedAtUtc { get; init; }
    }
}
