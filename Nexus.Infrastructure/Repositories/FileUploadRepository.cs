using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
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

        public async Task<FileUpload?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.FileUploads.FindAsync([id], ct);
        }

        public async Task<FileUpload?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct)
        {
            return await _context.FileUploads
                .Where(x => x.Id == id && x.UserId == userId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<FileUpload?> GetByBlobNameAsync(string blobName, CancellationToken ct)
        {
            return await _context.FileUploads
                .Where(x => x.BlobName == blobName)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<FileUpload>> GetExpiredPendingAsync(CancellationToken ct)
        {
            return await _context.FileUploads
                .Where(x => x.Status == UploadStatus.Pending && x.SasExpiresAtUtc < DateTimeOffset.UtcNow)
                .ToListAsync(ct);
        }

        public async Task<List<FileUpload>> GetByPurposeAsync(UploadPurpose purpose, UploadStatus? status, CancellationToken ct)
        {
            return await _context.FileUploads
                .Where(x => x.Purpose == purpose && (status == null || x.Status == status))
                .OrderByDescending(x => x.CreatedAtUtc)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
