using Microsoft.Extensions.Logging;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class SasExpiryJob : ISasExpiryJob
    {
        private readonly IFileUploadRepository _fileUploadRepository;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SasExpiryJob> _logger;

        public SasExpiryJob(
            IFileUploadRepository fileUploadRepository,
            IUnitOfWork uow,
            ILogger<SasExpiryJob> logger)
        {
            _fileUploadRepository = fileUploadRepository;
            _uow = uow;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var expired = await _fileUploadRepository.GetExpiredPendingAsync(CancellationToken.None);

            if (expired.Count == 0)
                return;

            var now = DateTimeOffset.UtcNow;
            foreach (var record in expired)
            {
                record.Status = UploadStatus.Expired;
                record.UpdatedAtUtc = now;
                _fileUploadRepository.Update(record);
            }

            await _uow.SaveChanges();

            _logger.LogInformation("Marked {Count} pending uploads as expired.", expired.Count);
        }
    }
}
