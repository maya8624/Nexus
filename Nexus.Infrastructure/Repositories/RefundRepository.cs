using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;
using Nexus.Application.Interfaces.Repository;

namespace Nexus.Infrastructure.Repositories
{
    public class RefundRepository : RepositoryBase<Refund>, IRefundRepository
    {
        private readonly AppDbContext _context;

        public RefundRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
