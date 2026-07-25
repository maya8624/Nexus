using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IIngestCursorRepository : IRepositoryBase<IngestCursor>
    {
        Task<IngestCursor?> GetAsync(string source, CancellationToken ct);
    }
}
