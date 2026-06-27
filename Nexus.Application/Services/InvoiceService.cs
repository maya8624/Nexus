using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IUnitOfWork _uow;

        public InvoiceService(IInvoiceRepository invoiceRepository, IUnitOfWork uow)
        {
            _invoiceRepository = invoiceRepository;
            _uow = uow;
        }

        public async Task<Result<InvoiceResponse>> GetByFileUploadIdAsync(Guid fileUploadId, Guid userId, CancellationToken ct)
        {
            var invoice = await _invoiceRepository.GetByFileUploadIdAsync(fileUploadId, userId, ct);
            if (invoice is null)
                return Result<InvoiceResponse>.NotFound("InvoiceNotFound", "Invoice not found for the given file upload.");

            return Result<InvoiceResponse>.Success(MapToResponse(invoice));
        }

        public async Task<Result<InvoiceResponse>> UpdateAsync(Guid id, UpdateInvoiceRequest request, Guid userId, CancellationToken ct)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id, userId, ct);
            if (invoice is null)
                return Result<InvoiceResponse>.NotFound("InvoiceNotFound", "Invoice not found.");

            invoice.VendorName    = request.VendorName;
            invoice.VendorAddress = request.VendorAddress;
            invoice.CustomerName  = request.CustomerName;
            invoice.InvoiceNumber = request.InvoiceNumber;
            invoice.InvoiceDate   = request.InvoiceDate;
            invoice.DueDate       = request.DueDate;
            invoice.Subtotal      = request.Subtotal;
            invoice.Tax           = request.Tax;
            invoice.Total         = request.Total;
            invoice.Currency      = request.Currency;

            if (request.LineItems is not null)
            {
                invoice.LineItems = request.LineItems.Select(li => new InvoiceLineItem
                {
                    Description = li.Description,
                    Quantity    = li.Quantity,
                    UnitPrice   = li.UnitPrice,
                    Amount      = li.Amount
                }).ToList();
            }

            _invoiceRepository.Update(invoice);
            await _uow.SaveChanges();

            return Result<InvoiceResponse>.Success(MapToResponse(invoice));
        }

        private static InvoiceResponse MapToResponse(Invoice invoice) => new()
        {
            Id            = invoice.Id,
            UserId        = invoice.UserId,
            FileUploadId  = invoice.FileUploadId,
            DocumentType  = invoice.DocumentType,
            Filename      = invoice.Filename,
            VendorName    = invoice.VendorName,
            VendorAddress = invoice.VendorAddress,
            CustomerName  = invoice.CustomerName,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate   = invoice.InvoiceDate,
            DueDate       = invoice.DueDate,
            Subtotal      = invoice.Subtotal,
            Tax           = invoice.Tax,
            Total         = invoice.Total,
            Currency      = invoice.Currency,
            Confidence    = invoice.Confidence,
            LineItems     = invoice.LineItems.Select(li => new InvoiceLineItemResponse
            {
                Description = li.Description,
                Quantity    = li.Quantity,
                UnitPrice   = li.UnitPrice,
                Amount      = li.Amount
            }).ToList(),
            CreatedAtUtc  = invoice.CreatedAtUtc
        };
    }
}
