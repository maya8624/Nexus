using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IDepositRepository : IRepositoryBase<Deposit>
    {
        Task<Deposit?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
        Task<Deposit?> GetByListingIdAsync(Guid listingId, Guid userId, CancellationToken ct);
        Task<Deposit?> GetByStripeSessionIdAsync(string stripeSessionId, CancellationToken ct);
        Task<Deposit?> GetByClientIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken ct);
    }
}
