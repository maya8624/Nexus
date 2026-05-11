using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class RefreshTokenRepository : RepositoryBase<RefreshToken>, IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<RefreshToken?> GetByTokenHash(string tokenHash, CancellationToken ct = default)
        { 
            return _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
        
        }
    }
}
