using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class DepositRepository : RepositoryBase<Deposit>, IDepositRepository
    {
        private readonly AppDbContext _context;

        public DepositRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Deposit?> GetByClientIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken ct)
        {
            return await _context.Deposits
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IdempotencyKey == idempotencyKey)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Deposit?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.Deposits
                .AsNoTracking()
                .Where(x => x.Id == id && x.UserId == userId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Deposit?> GetByListingIdAsync(Guid listingId, Guid userId, CancellationToken ct)
        {
            return await _context.Deposits
                .AsNoTracking()
                .Where(x => x.ListingId == listingId)
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Deposit?> GetByStripeSessionIdAsync(string stripeSessionId, CancellationToken ct)
        {
            return await _context.Deposits
                .AsNoTracking()
                .Where(x => x.StripeSessionId == stripeSessionId)
                .FirstOrDefaultAsync(ct);
        }
    }
}
