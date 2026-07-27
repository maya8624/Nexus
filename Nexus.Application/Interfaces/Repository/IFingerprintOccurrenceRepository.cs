using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IFingerprintOccurrenceRepository : IRepositoryBase<FingerprintOccurrence>
    {
        Task<IList<FingerprintOccurrence>> GetRecentAsync(string fingerprintId, int count, CancellationToken ct);
    }
}
