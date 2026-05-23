using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class DocumentSuggestionRepository : RepositoryBase<DocumentSuggestion>, IDocumentSuggestionRepository
    {
        private readonly AppDbContext _context;

        public DocumentSuggestionRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<DocumentSuggestion?> GetByDocIdAsync(Guid docId, Guid userId, CancellationToken ct)
        {
            return await _context.DocumentSuggestions
                .AsNoTracking()
                .Where(x => x.DocId == docId && x.UserId == userId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IList<DocumentSuggestion>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _context.DocumentSuggestions
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);
        }
    }
}
