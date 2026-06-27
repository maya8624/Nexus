namespace Nexus.Application.Dtos.Responses
{
    // snake_case to match Python response schema exactly
    public class AiInvoiceExtractionResponse
    {
        public string tool_name { get; set; } = default!;
        public bool success { get; set; }
        public string? filename { get; set; }
        public AiInvoiceData data { get; set; } = default!;
    }

    public class AiInvoiceData
    {
        public string? doc_type { get; set; }
        public string? vendor_name { get; set; }
        public string? vendor_address { get; set; }
        public string? customer_name { get; set; }
        public string? invoice_id { get; set; }
        public string? invoice_date { get; set; }
        public string? due_date { get; set; }
        public float? subtotal { get; set; }
        public float? tax { get; set; }
        public float? total { get; set; }
        public string? currency { get; set; }
        public float confidence { get; set; }
        public List<AiInvoiceLineItem> line_items { get; set; } = [];
    }

    public class AiInvoiceLineItem
    {
        public string? description { get; set; }
        public float? quantity { get; set; }
        public float? unit_price { get; set; }
        public float? amount { get; set; }
    }
}
