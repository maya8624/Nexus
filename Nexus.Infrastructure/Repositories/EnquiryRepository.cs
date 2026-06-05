using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class EnquiryRepository : RepositoryBase<Enquiry>, IEnquiryRepository
    {
        private readonly AppDbContext _context;

        public EnquiryRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Enquiry?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.Enquiries
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.Id == id && x.UserId == userId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Enquiry?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Enquiries
                .AsNoTracking()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Enquiry?> GetByIdForUpdateAsync(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.Enquiries
                .Where(x => x.Id == id && x.UserId == userId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Enquiry?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
        {
            return await _context.Enquiries
                .Include(x => x.User)
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<Enquiry>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _context.Enquiries
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Enquiry>> GetByAgentIdAsync(Guid agentId, CancellationToken ct)
        {
            return await _context.Enquiries
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.AgentId == agentId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);
        }
    }
}
