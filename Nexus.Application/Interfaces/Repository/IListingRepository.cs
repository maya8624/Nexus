using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IListingRepository : IRepositoryBase<Listing>
    {
        Task<Listing?> GetByIdAsync(Guid id, CancellationToken ct);
    }
}
