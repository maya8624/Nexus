namespace Nexus.Application.Dtos.Responses
{
    public class InvoiceExtractionResponse
    {
        public bool Success { get; set; }
        public string? Filename { get; set; }
        public InvoiceDataDto? Data { get; set; }
    }

    public class InvoiceDataDto
    {
        public string? DocType { get; set; }
        public string? VendorName { get; set; }
        public string? VendorAddress { get; set; }
        public string? CustomerName { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateOnly? InvoiceDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Total { get; set; }
        public string? Currency { get; set; }
        public double Confidence { get; set; }
        public List<InvoiceLineItemDto> LineItems { get; set; } = [];
    }

    public class InvoiceLineItemDto
    {
        public string? Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Amount { get; set; }
    }
}
