using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IFileUploadRepository : IRepositoryBase<FileUpload>
    {
        Task<FileUpload?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<FileUpload?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct);
        Task<FileUpload?> GetByBlobNameAsync(string blobName, CancellationToken ct);
        Task<List<FileUpload>> GetExpiredPendingAsync(CancellationToken ct);
        Task<List<FileUpload>> GetByPurposeAsync(UploadPurpose purpose, UploadStatus? status, CancellationToken ct);
    }
}
