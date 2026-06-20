using Hangfire;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class FileUploadServiceTests
    {
        private const string GeneralContainer   = "uploads";
        private const string ExtractionContainer = "extraction";
        private const string IngestionContainer  = "ingestion";
        private const string InvoiceContainer    = "invoices";
        private const int SasExpiryMinutes       = 15;

        private readonly Mock<IBlobStorageService> _blobStorageMock = new();
        private readonly Mock<IFileUploadRepository> _repositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IBackgroundJobClient> _jobsMock = new();
        private readonly FileUploadService _service;
        private readonly Guid _userId = Guid.NewGuid();

        public FileUploadServiceTests()
        {
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _repositoryMock.Setup(x => x.Create(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var settings = Options.Create(new BlobStorageSettings
            {
                ConnectionString        = "UseDevelopmentStorage=true",
                ContainerName           = GeneralContainer,
                ExtractionContainerName = ExtractionContainer,
                IngestionContainerName  = IngestionContainer,
                InvoiceContainerName    = InvoiceContainer,
                SasExpiryMinutes        = SasExpiryMinutes
            });

            _service = new FileUploadService(_blobStorageMock.Object, _repositoryMock.Object, _uowMock.Object, settings, _jobsMock.Object);
        }

        private void SetupBlobSuccess(string sasUrl = "https://blob.example.com/file?sig=x", string blobName = "user/guid.pdf") =>
            _blobStorageMock
                .Setup(x => x.GenerateSasUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SasUploadResponse>.Success(new SasUploadResponse { SasUrl = sasUrl, BlobName = blobName }));

        private void SetupBlobFailure() =>
            _blobStorageMock
                .Setup(x => x.GenerateSasUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SasUploadResponse>.Conflict("SasFailed", "error"));

        private static FileUpload BuildPendingRecord(
            Guid? userId = null,
            UploadPurpose purpose = UploadPurpose.General,
            string containerName = GeneralContainer,
            DateTimeOffset? sasExpiresAtUtc = null) => new()
        {
            Id              = Guid.NewGuid(),
            UserId          = userId ?? Guid.NewGuid(),
            FileName        = "document.pdf",
            BlobName        = "user/guid.pdf",
            ContentType     = "application/pdf",
            ContainerName   = containerName,
            Purpose         = purpose,
            Status          = UploadStatus.Pending,
            SasExpiresAtUtc = sasExpiresAtUtc ?? DateTimeOffset.UtcNow.AddMinutes(15),
            CreatedAtUtc    = DateTimeOffset.UtcNow,
            UpdatedAtUtc    = DateTimeOffset.UtcNow
        };

        #region InitiateAsync

        [Fact]
        public async Task InitiateAsync_GeneralPurpose_ShouldRouteToGeneralContainer()
        {
            SetupBlobSuccess();
            string? capturedContainer = null;
            _blobStorageMock
                .Setup(x => x.GenerateSasUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, Guid, CancellationToken>((_, _, container, _, _) => capturedContainer = container)
                .ReturnsAsync(Result<SasUploadResponse>.Success(new SasUploadResponse { SasUrl = "https://x", BlobName = "b" }));

            await _service.InitiateAsync("photo.jpg", "image/jpeg", UploadPurpose.General, _userId, CancellationToken.None);

            Assert.Equal(GeneralContainer, capturedContainer);
        }

        [Fact]
        public async Task InitiateAsync_ExtractionPurpose_ShouldRouteToExtractionContainer()
        {
            string? capturedContainer = null;
            _blobStorageMock
                .Setup(x => x.GenerateSasUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, Guid, CancellationToken>((_, _, container, _, _) => capturedContainer = container)
                .ReturnsAsync(Result<SasUploadResponse>.Success(new SasUploadResponse { SasUrl = "https://x", BlobName = "b" }));

            await _service.InitiateAsync("photo.jpg", "image/jpeg", UploadPurpose.Extraction, _userId, CancellationToken.None);

            Assert.Equal(ExtractionContainer, capturedContainer);
        }

        [Fact]
        public async Task InitiateAsync_IngestionPurpose_ShouldRouteToIngestionContainer()
        {
            string? capturedContainer = null;
            _blobStorageMock
                .Setup(x => x.GenerateSasUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, Guid, CancellationToken>((_, _, container, _, _) => capturedContainer = container)
                .ReturnsAsync(Result<SasUploadResponse>.Success(new SasUploadResponse { SasUrl = "https://x", BlobName = "b" }));

            await _service.InitiateAsync("lease.pdf", "application/pdf", UploadPurpose.Ingestion, _userId, CancellationToken.None);

            Assert.Equal(IngestionContainer, capturedContainer);
        }

        [Fact]
        public async Task InitiateAsync_InvoicePurpose_ShouldRouteToInvoiceContainer()
        {
            string? capturedContainer = null;
            _blobStorageMock
                .Setup(x => x.GenerateSasUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, Guid, CancellationToken>((_, _, container, _, _) => capturedContainer = container)
                .ReturnsAsync(Result<SasUploadResponse>.Success(new SasUploadResponse { SasUrl = "https://x", BlobName = "b" }));

            await _service.InitiateAsync("invoice.pdf", "application/pdf", UploadPurpose.Invoice, _userId, CancellationToken.None);

            Assert.Equal(InvoiceContainer, capturedContainer);
        }

        [Fact]
        public async Task InitiateAsync_WhenBlobFails_ShouldReturnConflict()
        {
            SetupBlobFailure();

            var result = await _service.InitiateAsync("lease.pdf", "application/pdf", UploadPurpose.Ingestion, _userId, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("SasGenerationFailed", Assert.Single(result.Errors).Code);
            _repositoryMock.Verify(x => x.Create(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task InitiateAsync_ShouldReturnSasUrlAndBlobNameFromBlobService()
        {
            SetupBlobSuccess("https://blob.example.com/file?sig=abc", "user/abc.pdf");

            var result = await _service.InitiateAsync("lease.pdf", "application/pdf", UploadPurpose.Ingestion, _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("https://blob.example.com/file?sig=abc", result.Value!.SasUrl);
            Assert.Equal("user/abc.pdf", result.Value.BlobName);
        }

        [Fact]
        public async Task InitiateAsync_ShouldReturnNonEmptyFileUploadId()
        {
            SetupBlobSuccess();

            var result = await _service.InitiateAsync("lease.pdf", "application/pdf", UploadPurpose.General, _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value!.FileUploadId);
        }

        [Fact]
        public async Task InitiateAsync_ShouldPersistRecordWithPendingStatus()
        {
            SetupBlobSuccess();
            FileUpload? captured = null;
            _repositoryMock
                .Setup(x => x.Create(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()))
                .Callback<FileUpload, CancellationToken>((r, _) => captured = r)
                .Returns(Task.CompletedTask);

            await _service.InitiateAsync("lease.pdf", "application/pdf", UploadPurpose.Ingestion, _userId, CancellationToken.None);

            Assert.NotNull(captured);
            Assert.Equal(UploadStatus.Pending, captured!.Status);
            Assert.Equal(_userId, captured.UserId);
            Assert.Equal("lease.pdf", captured.FileName);
            Assert.Equal("application/pdf", captured.ContentType);
            Assert.Equal(UploadPurpose.Ingestion, captured.Purpose);
        }

        [Fact]
        public async Task InitiateAsync_ShouldSetSasExpiryFromSettings()
        {
            SetupBlobSuccess();
            FileUpload? captured = null;
            _repositoryMock
                .Setup(x => x.Create(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()))
                .Callback<FileUpload, CancellationToken>((r, _) => captured = r)
                .Returns(Task.CompletedTask);

            var before = DateTimeOffset.UtcNow;
            await _service.InitiateAsync("lease.pdf", "application/pdf", UploadPurpose.General, _userId, CancellationToken.None);

            Assert.NotNull(captured);
            Assert.True(captured!.SasExpiresAtUtc >= before.AddMinutes(SasExpiryMinutes));
        }

        [Fact]
        public async Task InitiateAsync_ShouldNotSetIngestionStatus()
        {
            SetupBlobSuccess();
            FileUpload? captured = null;
            _repositoryMock
                .Setup(x => x.Create(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()))
                .Callback<FileUpload, CancellationToken>((r, _) => captured = r)
                .Returns(Task.CompletedTask);

            await _service.InitiateAsync("lease.pdf", "application/pdf", UploadPurpose.Ingestion, _userId, CancellationToken.None);

            Assert.Null(captured!.IngestionStatus);
        }

        [Fact]
        public async Task InitiateAsync_ShouldCallSaveChanges()
        {
            SetupBlobSuccess();

            await _service.InitiateAsync("lease.pdf", "application/pdf", UploadPurpose.General, _userId, CancellationToken.None);

            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        #endregion

        #region ConfirmAsync

        [Fact]
        public async Task ConfirmAsync_WhenRecordNotFound_ShouldReturnNotFound()
        {
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((FileUpload?)null);

            var result = await _service.ConfirmAsync(Guid.NewGuid(), _userId, null, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("FileUploadNotFound", Assert.Single(result.Errors).Code);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Theory]
        [InlineData(UploadStatus.Completed)]
        [InlineData(UploadStatus.Failed)]
        [InlineData(UploadStatus.Expired)]
        public async Task ConfirmAsync_WhenStatusIsNotPending_ShouldReturnConflict(UploadStatus status)
        {
            var record = BuildPendingRecord(_userId);
            record.Status = status;
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            var result = await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("InvalidStatus", Assert.Single(result.Errors).Code);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task ConfirmAsync_WhenSasExpired_ShouldReturnConflict()
        {
            var record = BuildPendingRecord(_userId, sasExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            var result = await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("SasExpired", Assert.Single(result.Errors).Code);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task ConfirmAsync_WithValidRecord_ShouldSetStatusToCompleted()
        {
            var record = BuildPendingRecord(_userId);
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            Assert.Equal(UploadStatus.Completed, record.Status);
            Assert.NotNull(record.CompletedAtUtc);
        }

        [Fact]
        public async Task ConfirmAsync_WithFileSizeBytes_ShouldPersistIt()
        {
            var record = BuildPendingRecord(_userId);
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.ConfirmAsync(record.Id, _userId, 204800, CancellationToken.None);

            Assert.Equal(204800, record.FileSizeBytes);
        }

        [Fact]
        public async Task ConfirmAsync_WithNoFileSizeBytes_ShouldLeaveItNull()
        {
            var record = BuildPendingRecord(_userId);
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            Assert.Null(record.FileSizeBytes);
        }

        [Fact]
        public async Task ConfirmAsync_WithExtractionPurpose_ShouldSetIngestionStatusToQueued()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Extraction, ExtractionContainer);
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            Assert.Equal(IngestionStatus.Queued, record.IngestionStatus);
        }

        [Fact]
        public async Task ConfirmAsync_WithInvoicePurpose_ShouldSetIngestionStatusToQueued()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Invoice, InvoiceContainer);
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            Assert.Equal(IngestionStatus.Queued, record.IngestionStatus);
        }

        [Fact]
        public async Task ConfirmAsync_WithIngestionPurpose_ShouldNotSetIngestionStatus()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Ingestion, IngestionContainer);
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            Assert.Null(record.IngestionStatus);
        }

        [Fact]
        public async Task ConfirmAsync_WithGeneralPurpose_ShouldNotSetIngestionStatus()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.General, GeneralContainer);
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            Assert.Null(record.IngestionStatus);
        }

        [Fact]
        public async Task ConfirmAsync_ShouldCallUpdateAndSaveChanges()
        {
            var record = BuildPendingRecord(_userId);
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            _repositoryMock.Verify(x => x.Update(record), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task ConfirmAsync_ShouldReturnFileUploadId()
        {
            var record = BuildPendingRecord(_userId);
            _repositoryMock
                .Setup(x => x.GetByIdForUserAsync(record.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            var result = await _service.ConfirmAsync(record.Id, _userId, null, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(record.Id, result.Value!.FileUploadId);
        }

        #endregion

        #region TriggerIngestionAsync

        [Fact]
        public async Task TriggerIngestionAsync_WhenRecordNotFound_ShouldReturnNotFound()
        {
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FileUpload?)null);

            var result = await _service.TriggerIngestionAsync("user/missing.pdf", CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("FileUploadNotFound", Assert.Single(result.Errors).Code);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task TriggerIngestionAsync_WhenRecordFound_ShouldSetIngestionStatusToQueued()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Ingestion, IngestionContainer);
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(record.BlobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.TriggerIngestionAsync(record.BlobName, CancellationToken.None);

            Assert.Equal(IngestionStatus.Queued, record.IngestionStatus);
        }

        [Fact]
        public async Task TriggerIngestionAsync_WhenRecordFound_ShouldEnqueueJob()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Ingestion, IngestionContainer);
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(record.BlobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.TriggerIngestionAsync(record.BlobName, CancellationToken.None);

            _jobsMock.Verify(x => x.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()), Times.Once);
        }

        [Fact]
        public async Task TriggerIngestionAsync_WhenRecordFound_ShouldCallUpdateAndSaveChanges()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Ingestion, IngestionContainer);
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(record.BlobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.TriggerIngestionAsync(record.BlobName, CancellationToken.None);

            _repositoryMock.Verify(x => x.Update(record), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task TriggerIngestionAsync_WhenRecordFound_ShouldReturnSuccess()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Ingestion, IngestionContainer);
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(record.BlobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            var result = await _service.TriggerIngestionAsync(record.BlobName, CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        #endregion

        #region TriggerInvoiceExtractionAsync

        [Fact]
        public async Task TriggerInvoiceExtractionAsync_WhenRecordNotFound_ShouldReturnNotFound()
        {
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FileUpload?)null);

            var result = await _service.TriggerInvoiceExtractionAsync("user/missing.pdf", CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("FileUploadNotFound", Assert.Single(result.Errors).Code);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task TriggerInvoiceExtractionAsync_WhenRecordFound_ShouldSetIngestionStatusToQueued()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Invoice, InvoiceContainer);
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(record.BlobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.TriggerInvoiceExtractionAsync(record.BlobName, CancellationToken.None);

            Assert.Equal(IngestionStatus.Queued, record.IngestionStatus);
        }

        [Fact]
        public async Task TriggerInvoiceExtractionAsync_WhenRecordFound_ShouldEnqueueJob()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Invoice, InvoiceContainer);
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(record.BlobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.TriggerInvoiceExtractionAsync(record.BlobName, CancellationToken.None);

            _jobsMock.Verify(x => x.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()), Times.Once);
        }

        [Fact]
        public async Task TriggerInvoiceExtractionAsync_WhenRecordFound_ShouldCallUpdateAndSaveChanges()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Invoice, InvoiceContainer);
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(record.BlobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            await _service.TriggerInvoiceExtractionAsync(record.BlobName, CancellationToken.None);

            _repositoryMock.Verify(x => x.Update(record), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task TriggerInvoiceExtractionAsync_WhenRecordFound_ShouldReturnSuccess()
        {
            var record = BuildPendingRecord(_userId, UploadPurpose.Invoice, InvoiceContainer);
            _repositoryMock
                .Setup(x => x.GetByBlobNameAsync(record.BlobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            var result = await _service.TriggerInvoiceExtractionAsync(record.BlobName, CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        #endregion
    }
}
