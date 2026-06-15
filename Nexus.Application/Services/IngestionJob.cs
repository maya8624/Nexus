using Microsoft.Extensions.Logging;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class IngestionJob : IIngestionJob
    {
        private readonly IFileUploadRepository _fileUploadRepository;
        private readonly IBlobStorageService _blobStorage;
        private readonly IAiService _aiService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<IngestionJob> _logger;

        public IngestionJob(
            IFileUploadRepository fileUploadRepository,
            IBlobStorageService blobStorage,
            IAiService aiService,
            IUnitOfWork uow,
            ILogger<IngestionJob> logger)
        {
            _fileUploadRepository = fileUploadRepository;
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
                _logger.LogWarning("Ingestion job skipped: FileUpload {FileUploadId} not found.", fileUploadId);
                return;
            }

            record.IngestionStatus = IngestionStatus.Processing;
            record.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _fileUploadRepository.Update(record);
            await _uow.SaveChanges();

            try
            {
                var bytes = await _blobStorage.DownloadBlobAsync(record.ContainerName, record.BlobName, CancellationToken.None);
                await _aiService.IngestDocumentAsync(bytes, record.FileName, null, null, CancellationToken.None);

                record.IngestionStatus = IngestionStatus.Completed;
                record.IngestedAtUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ingestion failed for FileUpload {FileUploadId}.", fileUploadId);
                record.IngestionStatus = IngestionStatus.Failed;
                record.IngestionError = ex.Message;
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
