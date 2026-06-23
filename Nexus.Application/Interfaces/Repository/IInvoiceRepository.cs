using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IInvoiceRepository : IRepositoryBase<Invoice>
    {
        Task<List<Invoice>> GetByUserIdAsync(Guid userId, CancellationToken ct);
        Task<Invoice?> GetByFileUploadIdAsync(Guid fileUploadId, Guid userId, CancellationToken ct);
        Task<Invoice?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
    }
}
