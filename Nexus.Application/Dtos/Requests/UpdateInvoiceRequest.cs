using FluentValidation;

namespace Nexus.Application.Dtos.Requests
{
    public sealed class UpdateInvoiceLineItemRequest
    {
        public string? Description { get; init; }
        public decimal? Quantity { get; init; }
        public decimal? UnitPrice { get; init; }
        public decimal? Amount { get; init; }
    }

    public sealed class UpdateInvoiceRequest
    {
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
        public List<UpdateInvoiceLineItemRequest>? LineItems { get; init; }
    }

    public sealed class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
    {
        public UpdateInvoiceRequestValidator()
        {
            RuleFor(x => x.VendorName).MaximumLength(500).When(x => x.VendorName != null);
            RuleFor(x => x.VendorAddress).MaximumLength(1000).When(x => x.VendorAddress != null);
            RuleFor(x => x.CustomerName).MaximumLength(500).When(x => x.CustomerName != null);
            RuleFor(x => x.InvoiceNumber).MaximumLength(100).When(x => x.InvoiceNumber != null);
            RuleFor(x => x.Currency).MaximumLength(10).When(x => x.Currency != null);
            RuleFor(x => x.Subtotal).GreaterThanOrEqualTo(0).When(x => x.Subtotal.HasValue);
            RuleFor(x => x.Tax).GreaterThanOrEqualTo(0).When(x => x.Tax.HasValue);
            RuleFor(x => x.Total).GreaterThanOrEqualTo(0).When(x => x.Total.HasValue);
            RuleForEach(x => x.LineItems).ChildRules(item =>
            {
                item.RuleFor(i => i.Description).MaximumLength(500).When(i => i.Description != null);
                item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(0).When(i => i.Quantity.HasValue);
                item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).When(i => i.UnitPrice.HasValue);
                item.RuleFor(i => i.Amount).GreaterThanOrEqualTo(0).When(i => i.Amount.HasValue);
            }).When(x => x.LineItems != null);
        }
    }
}
