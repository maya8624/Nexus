using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class IngestCursorRepository : RepositoryBase<IngestCursor>, IIngestCursorRepository
    {
        private readonly AppDbContext _context;

        public IngestCursorRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IngestCursor?> GetAsync(string source, CancellationToken ct)
        {
            return await Find(source, ct);
        }
    }
}
