using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Network.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    public class AiServiceTests
    {
        private readonly Mock<IHttpClientService> _httpClientServiceMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUserContext> _userContextMock = new();
        private readonly Mock<ILogger<AiService>> _loggerMock = new();
        private readonly AiService _service;
        private readonly Guid _userId = Guid.NewGuid();

        public AiServiceTests()
        {
            _userContextMock.Setup(x => x.UserId).Returns(_userId.ToString());

            var settings = Options.Create(new AiServiceSettings
            {
                BaseUrl = "http://localhost:8000",
                ApiKey = "test-key"
            });

            _service = new AiService(
                _httpClientServiceMock.Object,
                settings,
                _loggerMock.Object,
                _userContextMock.Object,
                _userRepositoryMock.Object);
        }

        [Fact]
        public async Task GetReply_WithMissingUser_ShouldReturnNotFound()
        {
            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _service.GetReply(new ChatRequest { Message = "Hello", ThreadId = "session-1" }, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("UserNotFound", Assert.Single(result.Errors).Code);
            _httpClientServiceMock.Verify(
                x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetReply_WithValidInput_ShouldReturnSuccess()
        {
            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiServiceResponse { Reply = "Hello!", thread_id = "session-1" });

            var result = await _service.GetReply(new ChatRequest { Message = "Hello", ThreadId = "session-1" }, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Hello!", result.Value!.Reply);
            Assert.Equal("session-1", result.Value.ThreadId);
        }

        [Fact]
        public async Task GetReply_WhenHttpRequestExceptionThrown_ShouldThrowAiServiceException()
        {
            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network error."));

            await Assert.ThrowsAsync<AiServiceException>(() =>
                _service.GetReply(new ChatRequest { Message = "Hello", ThreadId = "session-1" }, CancellationToken.None));
        }

        [Fact]
        public async Task GetReply_WhenTaskCanceledExceptionThrown_ShouldThrowAiServiceException()
        {
            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Request timed out."));

            await Assert.ThrowsAsync<AiServiceException>(() =>
                _service.GetReply(new ChatRequest { Message = "Hello", ThreadId = "session-1" }, CancellationToken.None));
        }

        [Fact]
        public async Task GetReply_WhenHttpRequestExceptionThrown_ShouldLogError()
        {
            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network error."));

            await Assert.ThrowsAsync<AiServiceException>(() =>
                _service.GetReply(new ChatRequest { Message = "Hello", ThreadId = "session-1" }, CancellationToken.None));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("session-1")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
