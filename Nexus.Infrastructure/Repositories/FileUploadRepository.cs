using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class FileUploadRepository : RepositoryBase<FileUpload>, IFileUploadRepository
    {
        private readonly AppDbContext _context;

        public FileUploadRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<FileUpload?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.FileUploads
                .Where(x => x.Id == id && x.UserId == userId)
                .FirstOrDefaultAsync(ct);
        }
    }
}
