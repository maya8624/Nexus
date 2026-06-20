using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IInvoiceRepository : IRepositoryBase<Invoice>
    {
        Task<List<Invoice>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    }
}
