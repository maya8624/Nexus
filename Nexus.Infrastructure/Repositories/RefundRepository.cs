using Nexus.Domain.Entities;
using Nexus.Infrastructure.Interfaces;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Repositories;

namespace Nexus.Infrastructure.Repositories
{
    public class RefundRepository : RepositoryBase<Refund>, IRefundRepository
    {
        private readonly NexusPayContext _context;

        public RefundRepository(NexusPayContext context) : base(context)
        {
            _context = context;
        }
    }
}
