using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;

namespace Nexus.Application.Interfaces.Business
{
    public interface IDocumentSuggestionService
    {
        Task<Result<DocumentSuggestionResponse>> SaveAsync(SaveDocumentSuggestionRequest request, CancellationToken ct);
        Task<Result<DocumentSuggestionResponse?>> GetByDocIdAsync(Guid docId, Guid userId, CancellationToken ct);
    }
}
