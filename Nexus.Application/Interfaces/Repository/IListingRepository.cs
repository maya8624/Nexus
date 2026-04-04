using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IListingRepository : IRepositoryBase<Listing>
    {
        Task<Listing?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Listing?> GetByTypeAsync(ListingType type, Guid id, CancellationToken ct);
    }
}
