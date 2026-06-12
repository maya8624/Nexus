using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IFileUploadRepository : IRepositoryBase<FileUpload>
    {
        Task<FileUpload?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct);
    }
}
