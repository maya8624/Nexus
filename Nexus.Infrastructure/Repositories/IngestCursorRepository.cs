using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class IngestCursorRepository : IIngestCursorRepository
    {
        private readonly AppDbContext _context;

        public IngestCursorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IngestCursor?> GetAsync(string source, CancellationToken ct)
        {
            return await _context.IngestCursors.FindAsync([source], ct);
        }

        public async Task Upsert(string source, DateTimeOffset lastPolledTo, CancellationToken ct)
        {
            var existing = await _context.IngestCursors.FindAsync([source], ct);
            var now = DateTimeOffset.UtcNow;

            if (existing is null)
            {
                await _context.IngestCursors.AddAsync(new IngestCursor
                {
                    Source = source,
                    LastPolledTo = lastPolledTo,
                    UpdatedAtUtc = now
                }, ct);
            }
            else
            {
                existing.LastPolledTo = lastPolledTo;
                existing.UpdatedAtUtc = now;
                _context.IngestCursors.Update(existing);
            }
        }
    }
}
