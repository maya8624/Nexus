using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class InvoiceRepository : RepositoryBase<Invoice>, IInvoiceRepository
    {
        private readonly AppDbContext _context;

        public InvoiceRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Invoice>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _context.Invoices
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<Invoice?> GetByFileUploadIdAsync(Guid fileUploadId, Guid userId, CancellationToken ct)
        {
            return await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FileUploadId == fileUploadId && x.UserId == userId, ct);
        }

        public async Task<Invoice?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.Invoices
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        }

    }
}
