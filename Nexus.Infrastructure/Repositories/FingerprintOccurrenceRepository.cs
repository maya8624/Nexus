using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Infrastructure.Repositories
{
    public class FingerprintOccurrenceRepository : RepositoryBase<FingerprintOccurrence>, IFingerprintOccurrenceRepository
    {
        public FingerprintOccurrenceRepository(AppDbContext context) : base(context)
        {
        }
    }
}
