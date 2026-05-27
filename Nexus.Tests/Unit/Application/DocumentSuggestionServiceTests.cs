using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class DocumentSuggestionServiceTests
    {
        private readonly Mock<IDocumentSuggestionRepository> _repositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly DocumentSuggestionService _service;
        private readonly Guid _userId = Guid.NewGuid();

        public DocumentSuggestionServiceTests()
        {
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _service = new DocumentSuggestionService(_repositoryMock.Object, _uowMock.Object);
        }

        private SaveDocumentSuggestionRequest BuildSaveRequest(Guid? docId = null) => new()
        {
            DocId       = docId ?? Guid.NewGuid(),
            UserId      = _userId,
            Suggestions = ["Use formal language in clause 3."],
            ModelUsed   = "claude-sonnet-4-6"
        };

        private static DocumentSuggestion BuildSuggestion(Guid docId, Guid userId) => new()
        {
            Id          = Guid.NewGuid(),
            DocId       = docId,
            UserId      = userId,
            Suggestions = ["Review clause 5 carefully."],
            ModelUsed   = "claude-sonnet-4-6",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        #region SaveAsync

        [Fact]
        public async Task SaveAsync_ShouldPersistAndReturnSuggestion()
        {
            _repositoryMock.Setup(x => x.Create(It.IsAny<DocumentSuggestion>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _service.SaveAsync(BuildSaveRequest(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(_userId, result.Value!.UserId);
            Assert.Contains("Use formal language in clause 3.", result.Value.Suggestions);
            Assert.Equal("claude-sonnet-4-6", result.Value.ModelUsed);
        }

        [Fact]
        public async Task SaveAsync_ShouldCallSaveChanges()
        {
            _repositoryMock.Setup(x => x.Create(It.IsAny<DocumentSuggestion>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await _service.SaveAsync(BuildSaveRequest(), CancellationToken.None);

            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_ShouldGenerateNewId()
        {
            DocumentSuggestion? captured = null;
            _repositoryMock
                .Setup(x => x.Create(It.IsAny<DocumentSuggestion>(), It.IsAny<CancellationToken>()))
                .Callback<DocumentSuggestion, CancellationToken>((s, _) => captured = s)
                .Returns(Task.CompletedTask);

            await _service.SaveAsync(BuildSaveRequest(), CancellationToken.None);

            Assert.NotNull(captured);
            Assert.NotEqual(Guid.Empty, captured!.Id);
        }

        [Fact]
        public async Task SaveAsync_ShouldMapDocIdAndUserIdFromRequest()
        {
            var docId   = Guid.NewGuid();
            DocumentSuggestion? captured = null;
            _repositoryMock
                .Setup(x => x.Create(It.IsAny<DocumentSuggestion>(), It.IsAny<CancellationToken>()))
                .Callback<DocumentSuggestion, CancellationToken>((s, _) => captured = s)
                .Returns(Task.CompletedTask);

            await _service.SaveAsync(BuildSaveRequest(docId), CancellationToken.None);

            Assert.Equal(docId, captured!.DocId);
            Assert.Equal(_userId, captured.UserId);
        }

        #endregion

        #region GetByDocIdAsync

        [Fact]
        public async Task GetByDocIdAsync_WhenFound_ShouldReturnSuggestion()
        {
            var docId      = Guid.NewGuid();
            var suggestion = BuildSuggestion(docId, _userId);
            _repositoryMock
                .Setup(x => x.GetByDocIdAsync(docId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(suggestion);

            var result = await _service.GetByDocIdAsync(docId, _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(docId, result.Value!.DocId);
            Assert.Equal(_userId, result.Value.UserId);
        }

        [Fact]
        public async Task GetByDocIdAsync_WhenNotFound_ShouldReturnSuccessWithNull()
        {
            _repositoryMock
                .Setup(x => x.GetByDocIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DocumentSuggestion?)null);

            var result = await _service.GetByDocIdAsync(Guid.NewGuid(), _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task GetByDocIdAsync_WhenFound_ShouldMapAllFields()
        {
            var docId      = Guid.NewGuid();
            var suggestion = BuildSuggestion(docId, _userId);
            _repositoryMock
                .Setup(x => x.GetByDocIdAsync(docId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(suggestion);

            var result = await _service.GetByDocIdAsync(docId, _userId, CancellationToken.None);

            Assert.Equal(suggestion.Id, result.Value!.Id);
            Assert.Equal(suggestion.Suggestions, result.Value.Suggestions);
            Assert.Equal(suggestion.ModelUsed, result.Value.ModelUsed);
            Assert.Equal(suggestion.CreatedAtUtc, result.Value.CreatedAtUtc);
        }

        #endregion
    }
}
