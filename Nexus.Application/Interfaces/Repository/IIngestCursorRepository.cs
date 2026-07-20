using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IIngestCursorRepository
    {
        Task<IngestCursor?> GetAsync(string source, CancellationToken ct);
        Task Upsert(string source, DateTimeOffset lastPolledTo, CancellationToken ct);
    }
}
