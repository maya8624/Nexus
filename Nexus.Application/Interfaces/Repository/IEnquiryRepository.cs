using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IEnquiryRepository : IRepositoryBase<Enquiry>
    {
        Task<Enquiry?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
        Task<Enquiry?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Enquiry?> GetByIdForUpdateAsync(Guid id, Guid userId, CancellationToken ct);
        Task<Enquiry?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);
        Task<IReadOnlyList<Enquiry>> GetByUserIdAsync(Guid userId, CancellationToken ct);
        Task<IReadOnlyList<Enquiry>> GetByAgentIdAsync(Guid agentId, CancellationToken ct);
    }
}
