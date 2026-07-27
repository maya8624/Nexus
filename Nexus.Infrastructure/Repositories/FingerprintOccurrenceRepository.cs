using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class FingerprintOccurrenceRepository : RepositoryBase<FingerprintOccurrence>, IFingerprintOccurrenceRepository
    {
        private readonly AppDbContext _context;

        public FingerprintOccurrenceRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IList<FingerprintOccurrence>> GetRecentAsync(string fingerprintId, int count, CancellationToken ct)
        {
            return await _context.FingerprintOccurrences
                .Where(x => x.FingerprintId == fingerprintId)
                .OrderByDescending(x => x.OccurredAt)
                .Take(count)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
