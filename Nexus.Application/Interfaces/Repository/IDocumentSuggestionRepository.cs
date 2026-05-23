using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IDocumentSuggestionRepository : IRepositoryBase<DocumentSuggestion>
    {
        Task<DocumentSuggestion?> GetByDocIdAsync(Guid docId, Guid userId, CancellationToken ct);
        Task<IList<DocumentSuggestion>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    }
}
