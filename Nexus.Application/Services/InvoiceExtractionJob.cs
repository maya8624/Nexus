using Microsoft.Extensions.Logging;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class InvoiceExtractionJob : IInvoiceExtractionJob
    {
        private readonly IFileUploadRepository _fileUploadRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IBlobStorageService _blobStorage;
        private readonly IAiService _aiService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<InvoiceExtractionJob> _logger;

        public InvoiceExtractionJob(
            IFileUploadRepository fileUploadRepository,
            IInvoiceRepository invoiceRepository,
            IBlobStorageService blobStorage,
            IAiService aiService,
            IUnitOfWork uow,
            ILogger<InvoiceExtractionJob> logger)
        {
            _fileUploadRepository = fileUploadRepository;
            _invoiceRepository = invoiceRepository;
            _blobStorage = blobStorage;
            _aiService = aiService;
            _uow = uow;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid fileUploadId)
        {
            var record = await _fileUploadRepository.GetByIdAsync(fileUploadId, CancellationToken.None);
            if (record is null)
            {
                _logger.LogWarning("Invoice extraction job skipped: FileUpload {FileUploadId} not found.", fileUploadId);
                return;
            }

            record.IngestionStatus = IngestionStatus.Processing;
            record.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _fileUploadRepository.Update(record);
            await _uow.SaveChanges();

            try
            {
                var bytes = await _blobStorage.DownloadBlobAsync(record.ContainerName, record.BlobName, CancellationToken.None);
                var result = await _aiService.ExtractInvoiceAsync(bytes, record.FileName, CancellationToken.None);

                if (!result.IsSuccess)
                    throw new AiServiceException("Invoice extraction returned a failed result.", null);

                var data = result.Value!.Data;

                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    UserId = record.UserId,
                    FileUploadId = record.Id,
                    DocumentType = data?.DocType == "receipt" ? DocumentType.Receipt : DocumentType.Invoice,
                    Filename = result.Value.Filename,
                    VendorName = data?.VendorName,
                    VendorAddress = data?.VendorAddress,
                    CustomerName = data?.CustomerName,
                    InvoiceNumber = data?.InvoiceNumber,
                    InvoiceDate = data?.InvoiceDate,
                    DueDate = data?.DueDate,
                    Subtotal = data?.Subtotal,
                    Tax = data?.Tax,
                    Total = data?.Total,
                    Currency = data?.Currency,
                    Confidence = data?.Confidence ?? 0,
                    LineItems = data?.LineItems.Select(li => new InvoiceLineItem
                    {
                        Description = li.Description,
                        Quantity = li.Quantity,
                        UnitPrice = li.UnitPrice,
                        Amount = li.Amount
                    }).ToList() ?? [],
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                await _invoiceRepository.Create(invoice, CancellationToken.None);

                record.IngestionStatus = IngestionStatus.Completed;
                record.IngestedAtUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice extraction failed for FileUpload {FileUploadId}.", fileUploadId);
                record.IngestionStatus = IngestionStatus.Failed;
                record.IngestionError = ex.Message;
                throw;
            }
            finally
            {
                record.UpdatedAtUtc = DateTimeOffset.UtcNow;
                _fileUploadRepository.Update(record);
                await _uow.SaveChanges();
            }
        }
    }
}
