using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services
{
    public class DocumentSuggestionService : IDocumentSuggestionService
    {
        private readonly IDocumentSuggestionRepository _repository;
        private readonly IUnitOfWork _uow;

        public DocumentSuggestionService(IDocumentSuggestionRepository repository, IUnitOfWork uow)
        {
            _repository = repository;
            _uow = uow;
        }

        public async Task<Result<DocumentSuggestionResponse>> SaveAsync(SaveDocumentSuggestionRequest request, CancellationToken ct)
        {
            var suggestion = new DocumentSuggestion
            {
                Id = Guid.NewGuid(),
                DocId = request.DocId,
                UserId = request.UserId,
                Suggestions = request.Suggestions,
                ModelUsed = request.ModelUsed,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            await _repository.Create(suggestion, ct);
            await _uow.SaveChanges();

            return Result<DocumentSuggestionResponse>.Success(Map(suggestion));
        }

        public async Task<Result<DocumentSuggestionResponse?>> GetByDocIdAsync(Guid docId, Guid userId, CancellationToken ct)
        {
            var suggestion = await _repository.GetByDocIdAsync(docId, userId, ct);
            if (suggestion is null)
                return Result<DocumentSuggestionResponse?>.Success(null);

            return Result<DocumentSuggestionResponse?>.Success(Map(suggestion));
        }

        private static DocumentSuggestionResponse Map(DocumentSuggestion s) => new()
        {
            Id = s.Id,
            DocId = s.DocId,
            UserId = s.UserId,
            Suggestions = s.Suggestions,
            ModelUsed = s.ModelUsed,
            CreatedAtUtc = s.CreatedAtUtc
        };
    }
}
