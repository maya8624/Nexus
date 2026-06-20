namespace Nexus.Domain.Entities
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? FileUploadId { get; set; }
        public string? Filename { get; set; }
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
        public List<InvoiceLineItem> LineItems { get; set; } = [];
        public DateTimeOffset CreatedAtUtc { get; set; }

        public User User { get; set; } = default!;
    }
}
