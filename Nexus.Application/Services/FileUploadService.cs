using Hangfire;
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
        private readonly IBackgroundJobClient _jobs;

        public FileUploadService(
            IBlobStorageService blobStorage,
            IFileUploadRepository fileUploadRepository,
            IUnitOfWork uow,
            IOptions<BlobStorageSettings> settings,
            IBackgroundJobClient jobs)
        {
            _blobStorage = blobStorage;
            _fileUploadRepository = fileUploadRepository;
            _uow = uow;
            _settings = settings.Value;
            _jobs = jobs;
        }


        public async Task<Result<FileUploadInitiatedResponse>> InitiateAsync(string fileName, string contentType, UploadPurpose purpose, Guid userId, CancellationToken ct)
        {
            var containerName = purpose switch
            {
                UploadPurpose.Extraction => _settings.ExtractionContainerName,
                UploadPurpose.Ingestion => _settings.IngestionContainerName,
                UploadPurpose.Invoice => _settings.InvoiceContainerName,
                _ => _settings.ContainerName
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

            if (record.Purpose is not UploadPurpose.General)
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

        public async Task<Result<bool>> TriggerIngestionAsync(string blobName, CancellationToken ct)
        {
            var record = await _fileUploadRepository.GetByBlobNameAsync(blobName, ct);
            if (record is null)
                return Result<bool>.NotFound("FileUploadNotFound", "FileUpload record not found for the given blob.");

            record.IngestionStatus = IngestionStatus.Queued;
            record.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _fileUploadRepository.Update(record);
            await _uow.SaveChanges();

            _jobs.Enqueue<IIngestionJob>(job => job.ExecuteAsync(record.Id));

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<FileUploadResponse>>> GetByPurposeAsync(UploadPurpose purpose, UploadStatus? status, CancellationToken ct)
        {
            var records = await _fileUploadRepository.GetByPurposeAsync(purpose, status, ct);

            var response = records.Select(r => new FileUploadResponse
            {
                Id = r.Id,
                UserId = r.UserId,
                FileName = r.FileName,
                BlobName = r.BlobName,
                ContentType = r.ContentType,
                FileSizeBytes = r.FileSizeBytes,
                Purpose = r.Purpose,
                Status = r.Status,
                IngestionStatus = r.IngestionStatus,
                IngestionError = r.IngestionError,
                CompletedAtUtc = r.CompletedAtUtc,
                IngestedAtUtc = r.IngestedAtUtc,
                CreatedAtUtc = r.CreatedAtUtc
            }).ToList();

            return Result<List<FileUploadResponse>>.Success(response);
        }

        public async Task<Result<bool>> TriggerInvoiceExtractionAsync(string blobName, CancellationToken ct)
        {
            var record = await _fileUploadRepository.GetByBlobNameAsync(blobName, ct);
            if (record is null)
                return Result<bool>.NotFound("FileUploadNotFound", "FileUpload record not found for the given blob.");

            record.IngestionStatus = IngestionStatus.Queued;
            record.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _fileUploadRepository.Update(record);
            await _uow.SaveChanges();

            _jobs.Enqueue<IInvoiceExtractionJob>(job => job.ExecuteAsync(record.Id));

            return Result<bool>.Success(true);
        }
    }
}
