using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
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
    }
}
