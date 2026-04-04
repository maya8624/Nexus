using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class ListingRepository : RepositoryBase<Listing>, IListingRepository
    {
        private readonly AppDbContext _context;

        public ListingRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Listing?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Listings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<Listing?> GetByTypeAsync(ListingType type, Guid id, CancellationToken ct)
        {
            return await _context.Listings
                .AsNoTracking()
                .Where(x =>
                    x.ListingType == type &&
                    x.Id == id &&
                    x.IsDeleted == false)
                .FirstOrDefaultAsync(ct);
        }
    }
}
