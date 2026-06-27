using Microsoft.Extensions.Logging;
using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class InvoiceExtractionJobTests
    {
        private readonly Mock<IFileUploadRepository> _fileUploadRepositoryMock = new();
        private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock = new();
        private readonly Mock<IBlobStorageService> _blobStorageMock = new();
        private readonly Mock<IAiService> _aiServiceMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ILogger<InvoiceExtractionJob>> _loggerMock = new();
        private readonly InvoiceExtractionJob _job;
        private readonly Guid _userId = Guid.NewGuid();

        public InvoiceExtractionJobTests()
        {
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _invoiceRepositoryMock.Setup(x => x.Create(It.IsAny<Invoice>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _job = new InvoiceExtractionJob(
                _fileUploadRepositoryMock.Object,
                _invoiceRepositoryMock.Object,
                _blobStorageMock.Object,
                _aiServiceMock.Object,
                _uowMock.Object,
                _loggerMock.Object);
        }

        private FileUpload BuildRecord() => new()
        {
            Id              = Guid.NewGuid(),
            UserId          = _userId,
            FileName        = "invoice.pdf",
            BlobName        = "user/invoice.pdf",
            ContainerName   = "invoices",
            ContentType     = "application/pdf",
            Purpose         = UploadPurpose.Invoice,
            Status          = UploadStatus.Completed,
            IngestionStatus = IngestionStatus.Queued,
            SasExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
            CreatedAtUtc    = DateTimeOffset.UtcNow,
            UpdatedAtUtc    = DateTimeOffset.UtcNow
        };

        private static Result<InvoiceExtractionResponse> BuildSuccessResult(string vendorName = "Acme Plumbing") =>
            Result<InvoiceExtractionResponse>.Success(new InvoiceExtractionResponse
            {
                Success  = true,
                Filename = "invoice.pdf",
                Data = new InvoiceDataDto
                {
                    VendorName    = vendorName,
                    VendorAddress = "12 George St, Sydney NSW 2000",
                    CustomerName  = "Sunshine Realty",
                    InvoiceNumber = "INV-0042",
                    InvoiceDate   = new DateOnly(2026, 6, 1),
                    DueDate       = new DateOnly(2026, 6, 15),
                    Subtotal      = 450m,
                    Tax           = 45m,
                    Total         = 495m,
                    Currency      = "$",
                    Confidence    = 0.97,
                    LineItems     =
                    [
                        new InvoiceLineItemDto
                        {
                            Description = "Emergency pipe repair",
                            Quantity    = 1m,
                            UnitPrice   = 350m,
                            Amount      = 350m
                        }
                    ]
                }
            });

        [Fact]
        public async Task ExecuteAsync_WhenRecordNotFound_ShouldReturnWithoutProcessing()
        {
            _fileUploadRepositoryMock
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FileUpload?)null);

            await _job.ExecuteAsync(Guid.NewGuid());

            _blobStorageMock.Verify(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _aiServiceMock.Verify(x => x.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSuccess_ShouldSetProcessingThenCompleted()
        {
            var record = BuildRecord();
            _fileUploadRepositoryMock.Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
            _blobStorageMock.Setup(x => x.DownloadBlobAsync(record.ContainerName, record.BlobName, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
            _aiServiceMock.Setup(x => x.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildSuccessResult());

            await _job.ExecuteAsync(record.Id);

            Assert.Equal(IngestionStatus.Completed, record.IngestionStatus);
            Assert.NotNull(record.IngestedAtUtc);
            Assert.Null(record.IngestionError);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSuccess_ShouldSaveChangesTwice()
        {
            var record = BuildRecord();
            _fileUploadRepositoryMock.Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
            _blobStorageMock.Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
            _aiServiceMock.Setup(x => x.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildSuccessResult());

            await _job.ExecuteAsync(record.Id);

            _uowMock.Verify(x => x.SaveChanges(), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteAsync_WhenSuccess_ShouldPersistInvoice()
        {
            var record = BuildRecord();
            _fileUploadRepositoryMock.Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
            _blobStorageMock.Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
            _aiServiceMock.Setup(x => x.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildSuccessResult());

            await _job.ExecuteAsync(record.Id);

            _invoiceRepositoryMock.Verify(x => x.Create(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSuccess_ShouldMapInvoiceFieldsCorrectly()
        {
            var record = BuildRecord();
            _fileUploadRepositoryMock.Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
            _blobStorageMock.Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
            _aiServiceMock.Setup(x => x.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildSuccessResult());

            Invoice? captured = null;
            _invoiceRepositoryMock
                .Setup(x => x.Create(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
                .Callback<Invoice, CancellationToken>((inv, _) => captured = inv)
                .Returns(Task.CompletedTask);

            await _job.ExecuteAsync(record.Id);

            Assert.NotNull(captured);
            Assert.Equal(_userId, captured!.UserId);
            Assert.Equal(record.Id, captured.FileUploadId);
            Assert.Equal("Acme Plumbing", captured.VendorName);
            Assert.Equal("INV-0042", captured.InvoiceNumber);
            Assert.Equal(new DateOnly(2026, 6, 1), captured.InvoiceDate);
            Assert.Equal(495m, captured.Total);
            Assert.Equal(0.97, captured.Confidence, precision: 2);
            Assert.Single(captured.LineItems);
        }

        [Fact]
        public async Task ExecuteAsync_WhenBlobDownloadFails_ShouldSetFailed()
        {
            var record = BuildRecord();
            _fileUploadRepositoryMock.Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
            _blobStorageMock.Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Blob not found."));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _job.ExecuteAsync(record.Id));

            Assert.Equal(IngestionStatus.Failed, record.IngestionStatus);
            Assert.Equal("Blob not found.", record.IngestionError);
            Assert.Null(record.IngestedAtUtc);
        }

        [Fact]
        public async Task ExecuteAsync_WhenAiServiceThrows_ShouldSetFailed()
        {
            var record = BuildRecord();
            _fileUploadRepositoryMock.Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
            _blobStorageMock.Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
            _aiServiceMock.Setup(x => x.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AiServiceException("AI unavailable.", new Exception("upstream")));

            await Assert.ThrowsAsync<AiServiceException>(() => _job.ExecuteAsync(record.Id));

            Assert.Equal(IngestionStatus.Failed, record.IngestionStatus);
            Assert.NotNull(record.IngestionError);
        }

        [Fact]
        public async Task ExecuteAsync_WhenAiServiceReturnsFailedResult_ShouldSetFailed()
        {
            var record = BuildRecord();
            _fileUploadRepositoryMock.Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
            _blobStorageMock.Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
            _aiServiceMock.Setup(x => x.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<InvoiceExtractionResponse>.ValidationError(new ResultError("ExtractionFailed", "Extraction failed.")));

            await Assert.ThrowsAsync<AiServiceException>(() => _job.ExecuteAsync(record.Id));

            Assert.Equal(IngestionStatus.Failed, record.IngestionStatus);
            _invoiceRepositoryMock.Verify(x => x.Create(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenFails_ShouldStillSaveChangesTwice()
        {
            var record = BuildRecord();
            _fileUploadRepositoryMock.Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
            _blobStorageMock.Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Blob not found."));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _job.ExecuteAsync(record.Id));

            _uowMock.Verify(x => x.SaveChanges(), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSetProcessingBeforeCallingAiService()
        {
            var record = BuildRecord();
            IngestionStatus? statusAtCallTime = null;

            _fileUploadRepositoryMock.Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
            _blobStorageMock.Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
            _aiServiceMock
                .Setup(x => x.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback(() => statusAtCallTime = record.IngestionStatus)
                .ReturnsAsync(BuildSuccessResult());

            await _job.ExecuteAsync(record.Id);

            Assert.Equal(IngestionStatus.Processing, statusAtCallTime);
        }
    }
}
