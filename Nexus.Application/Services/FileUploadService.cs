using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IBlobStorageService _blobStorage;
        private readonly IFileUploadRepository _fileUploadRepository;
        private readonly IUnitOfWork _uow;
        private readonly BlobStorageSettings _settings;

        public FileUploadService(
            IBlobStorageService blobStorage,
            IFileUploadRepository fileUploadRepository,
            IUnitOfWork uow,
            IOptions<BlobStorageSettings> settings)
        {
            _blobStorage = blobStorage;
            _fileUploadRepository = fileUploadRepository;
            _uow = uow;
            _settings = settings.Value;
        }

        public async Task<Result<FileUploadInitiatedResponse>> InitiateAsync(string fileName, string contentType, UploadPurpose purpose, Guid userId, CancellationToken ct)
        {
            var containerName = purpose switch
            {
                UploadPurpose.Extraction => _settings.ExtractionContainerName,
                UploadPurpose.Ingestion  => _settings.IngestionContainerName,
                _                        => _settings.ContainerName
            };

            var sasResult = await _blobStorage.GenerateSasUploadUrlAsync(fileName, contentType, containerName, userId, ct);
            if (!sasResult.IsSuccess)
                return Result<FileUploadInitiatedResponse>.Conflict("SasGenerationFailed", "Failed to generate upload URL.");

            var now = DateTimeOffset.UtcNow;
            var record = new FileUpload
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = fileName,
                BlobName = sasResult.Value!.BlobName,
                ContentType = contentType,
                ContainerName = containerName,
                Purpose = purpose,
                Status = UploadStatus.Pending,
                SasExpiresAtUtc = now.AddMinutes(_settings.SasExpiryMinutes),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _fileUploadRepository.Create(record, ct);
            await _uow.SaveChanges();

            return Result<FileUploadInitiatedResponse>.Success(new FileUploadInitiatedResponse
            {
                FileUploadId = record.Id,
                SasUrl = sasResult.Value.SasUrl,
                BlobName = record.BlobName,
                SasExpiresAtUtc = record.SasExpiresAtUtc
            });
        }

        public async Task<Result<FileUploadInitiatedResponse>> ConfirmAsync(Guid id, Guid userId, long? fileSizeBytes, CancellationToken ct)
        {
            var record = await _fileUploadRepository.GetByIdForUserAsync(id, userId, ct);
            if (record is null)
                return Result<FileUploadInitiatedResponse>.NotFound("FileUploadNotFound", "File upload record not found.");

            if (record.Status != UploadStatus.Pending)
                return Result<FileUploadInitiatedResponse>.Conflict("InvalidStatus", "Only pending uploads can be confirmed.");

            if (record.SasExpiresAtUtc < DateTimeOffset.UtcNow)
                return Result<FileUploadInitiatedResponse>.Conflict("SasExpired", "The upload window has expired.");

            var now = DateTimeOffset.UtcNow;
            record.Status = UploadStatus.Completed;
            record.CompletedAtUtc = now;
            record.UpdatedAtUtc = now;

            if (fileSizeBytes.HasValue)
                record.FileSizeBytes = fileSizeBytes;

            if (record.Purpose is UploadPurpose.Extraction or UploadPurpose.Ingestion)
                record.IngestionStatus = IngestionStatus.Queued;

            _fileUploadRepository.Update(record);
            await _uow.SaveChanges();

            return Result<FileUploadInitiatedResponse>.Success(new FileUploadInitiatedResponse
            {
                FileUploadId = record.Id,
                SasUrl = string.Empty,
                BlobName = record.BlobName,
                SasExpiresAtUtc = record.SasExpiresAtUtc
            });
        }
    }
}
