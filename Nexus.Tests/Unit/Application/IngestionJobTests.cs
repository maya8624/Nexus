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
    public class IngestionJobTests
    {
        private readonly Mock<IFileUploadRepository> _repositoryMock = new();
        private readonly Mock<IBlobStorageService> _blobStorageMock = new();
        private readonly Mock<IAiService> _aiServiceMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ILogger<IngestionJob>> _loggerMock = new();
        private readonly IngestionJob _job;

        public IngestionJobTests()
        {
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _job = new IngestionJob(
                _repositoryMock.Object,
                _blobStorageMock.Object,
                _aiServiceMock.Object,
                _uowMock.Object,
                _loggerMock.Object);
        }

        private static FileUpload BuildRecord(IngestionStatus? ingestionStatus = null) => new()
        {
            Id              = Guid.NewGuid(),
            UserId          = Guid.NewGuid(),
            FileName        = "lease.pdf",
            BlobName        = "user/lease.pdf",
            ContainerName   = "ingestion",
            ContentType     = "application/pdf",
            Purpose         = UploadPurpose.Ingestion,
            Status          = UploadStatus.Pending,
            IngestionStatus = ingestionStatus,
            SasExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
            CreatedAtUtc    = DateTimeOffset.UtcNow,
            UpdatedAtUtc    = DateTimeOffset.UtcNow
        };

        [Fact]
        public async Task ExecuteAsync_WhenRecordNotFound_ShouldReturnWithoutProcessing()
        {
            _repositoryMock
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FileUpload?)null);

            await _job.ExecuteAsync(Guid.NewGuid());

            _blobStorageMock.Verify(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _aiServiceMock.Verify(x => x.IngestDocumentAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSuccess_ShouldSetProcessingThenCompleted()
        {
            var record = BuildRecord();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);
            _blobStorageMock
                .Setup(x => x.DownloadBlobAsync(record.ContainerName, record.BlobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync([1, 2, 3]);
            _aiServiceMock
                .Setup(x => x.IngestDocumentAsync(It.IsAny<byte[]>(), It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DocumentIngestionResponse>.Success(new DocumentIngestionResponse("lease.pdf", null, null, 5, "ok")));

            await _job.ExecuteAsync(record.Id);

            Assert.Equal(IngestionStatus.Completed, record.IngestionStatus);
            Assert.NotNull(record.IngestedAtUtc);
            Assert.Null(record.IngestionError);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSuccess_ShouldSaveChangesTwice()
        {
            var record = BuildRecord();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);
            _blobStorageMock
                .Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([1, 2, 3]);
            _aiServiceMock
                .Setup(x => x.IngestDocumentAsync(It.IsAny<byte[]>(), It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DocumentIngestionResponse>.Success(new DocumentIngestionResponse("lease.pdf", null, null, 5, "ok")));

            await _job.ExecuteAsync(record.Id);

            _uowMock.Verify(x => x.SaveChanges(), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteAsync_WhenBlobDownloadFails_ShouldSetFailed()
        {
            var record = BuildRecord();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);
            _blobStorageMock
                .Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Blob not found."));

            await _job.ExecuteAsync(record.Id);

            Assert.Equal(IngestionStatus.Failed, record.IngestionStatus);
            Assert.Equal("Blob not found.", record.IngestionError);
            Assert.Null(record.IngestedAtUtc);
        }

        [Fact]
        public async Task ExecuteAsync_WhenAiServiceFails_ShouldSetFailed()
        {
            var record = BuildRecord();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);
            _blobStorageMock
                .Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([1, 2, 3]);
            _aiServiceMock
                .Setup(x => x.IngestDocumentAsync(It.IsAny<byte[]>(), It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AiServiceException("AI unavailable.", new Exception("upstream")));

            await _job.ExecuteAsync(record.Id);

            Assert.Equal(IngestionStatus.Failed, record.IngestionStatus);
            Assert.NotNull(record.IngestionError);
        }

        [Fact]
        public async Task ExecuteAsync_WhenFails_ShouldStillSaveChangesTwice()
        {
            var record = BuildRecord();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);
            _blobStorageMock
                .Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Blob not found."));

            await _job.ExecuteAsync(record.Id);

            _uowMock.Verify(x => x.SaveChanges(), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSetProcessingBeforeCallingAiService()
        {
            var record = BuildRecord();
            IngestionStatus? statusAtCallTime = null;

            _repositoryMock
                .Setup(x => x.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);
            _blobStorageMock
                .Setup(x => x.DownloadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([1, 2, 3]);
            _aiServiceMock
                .Setup(x => x.IngestDocumentAsync(It.IsAny<byte[]>(), It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
                .Callback(() => statusAtCallTime = record.IngestionStatus)
                .ReturnsAsync(Result<DocumentIngestionResponse>.Success(new DocumentIngestionResponse("lease.pdf", null, null, 5, "ok")));

            await _job.ExecuteAsync(record.Id);

            Assert.Equal(IngestionStatus.Processing, statusAtCallTime);
        }
    }
}
