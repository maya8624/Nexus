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
using System.Net;
using System.Text;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class AiServiceTests
    {
        private readonly Mock<IHttpClientService> _httpClientServiceMock = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
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
                ApiKey = "test-key",
                Chat = "api/chat",
                ChatStream = "api/chat/stream",
                Preferences = "api/preferences"
            });

            _service = new AiService(
                _httpClientServiceMock.Object,
                _httpClientFactoryMock.Object,
                settings,
                _loggerMock.Object,
                _userContextMock.Object,
                _userRepositoryMock.Object);
        }

        private void SetupUserExists(bool exists) =>
            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists);

        private static TenantPreferenceRequest BuildPreferenceRequest() => new()
        {
            Suburbs = ["Bondi", "Manly"],
            MaxRent = 600,
            MinBeds = 2,
            MaxBeds = 3,
            PetFriendly = false,
            AvailableWithinDays = 14
        };

        private static HttpClient CreateHttpClientWithResponse(HttpResponseMessage response) =>
            new(new MockHttpMessageHandler(response));

        #region GetReply

        [Fact]
        public async Task GetReply_WithMissingUser_ShouldReturnNotFound()
        {
            SetupUserExists(false);

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
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiServiceResponse { reply = "Hello!", thread_id = "session-1" });

            var result = await _service.GetReply(new ChatRequest { Message = "Hello", ThreadId = "session-1" }, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Hello!", result.Value!.Reply);
            Assert.Equal("session-1", result.Value.ThreadId);
        }

        [Fact]
        public async Task GetReply_WithNoThreadId_ShouldGenerateNewThreadId()
        {
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiServiceResponse { reply = "Hi!", thread_id = "generated-id" });

            var result = await _service.GetReply(new ChatRequest { Message = "Hello" }, CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetReply_WithListings_ShouldMapListingsCorrectly()
        {
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiServiceResponse
                {
                    reply = "Here are some listings",
                    thread_id = "session-1",
                    listings =
                    [
                        new() { property_id = "prop-1", property_url = "http://example.com/1", listing_Id = "list-1" }
                    ]
                });

            var result = await _service.GetReply(new ChatRequest { Message = "Show me listings", ThreadId = "session-1" }, CancellationToken.None);

            Assert.True(result.IsSuccess);
            var listing = Assert.Single(result.Value!.Listings);
            Assert.Equal("prop-1", listing.PropertyId);
            Assert.Equal("http://example.com/1", listing.PropertyUrl);
            Assert.Equal("list-1", listing.ListingId);
        }

        [Fact]
        public async Task GetReply_WhenHttpRequestExceptionThrown_ShouldThrowAiServiceException()
        {
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network error."));

            await Assert.ThrowsAsync<AiServiceException>(() =>
                _service.GetReply(new ChatRequest { Message = "Hello", ThreadId = "session-1" }, CancellationToken.None));
        }

        [Fact]
        public async Task GetReply_WhenTaskCanceledExceptionThrown_ShouldThrowAiServiceException()
        {
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiServiceResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Request timed out."));

            await Assert.ThrowsAsync<AiServiceException>(() =>
                _service.GetReply(new ChatRequest { Message = "Hello", ThreadId = "session-1" }, CancellationToken.None));
        }

        [Fact]
        public async Task GetReply_WhenHttpRequestExceptionThrown_ShouldLogError()
        {
            SetupUserExists(true);
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

        #endregion

        #region GetPreferenceProperties

        [Fact]
        public async Task GetPreferenceProperties_WithMissingUser_ShouldReturnNotFound()
        {
            SetupUserExists(false);

            var result = await _service.GetPreferenceProperties(BuildPreferenceRequest(), _userId, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("UserNotFound", Assert.Single(result.Errors).Code);
            _httpClientServiceMock.Verify(
                x => x.ExecuteRequest<AiTenantPreferenceSearchResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetPreferenceProperties_WithValidInput_ShouldReturnSuccess()
        {
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiTenantPreferenceSearchResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiTenantPreferenceSearchResponse
                {
                    Message = "Found 2 listings",
                    Listings = [new AiListingResult { ListingId = "prop-1" }],
                    DisplayCount = 1,
                    TotalCount = 2,
                    HasMore = true
                });

            var result = await _service.GetPreferenceProperties(BuildPreferenceRequest(), _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Found 2 listings", result.Value!.Message);
            Assert.Equal(1, result.Value.DisplayCount);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.True(result.Value.HasMore);
        }

        [Fact]
        public async Task GetPreferenceProperties_WithValidInput_ShouldMapListingsToListingItem()
        {
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiTenantPreferenceSearchResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiTenantPreferenceSearchResponse
                {
                    Message = "Found 1 listing",
                    Listings =
                    [
                        new AiListingResult
                        {
                            ListingId = "listing-1",
                            ImageUrl = "https://example.com/img.jpg",
                            AddressLine1 = "123 Main St",
                            Suburb = "Sydney",
                            Bedrooms = 3,
                            Bathrooms = 2,
                            Price = 2500m,
                            BuildingSizeSqm = 120m,
                            PropertyType = "Apartment"
                        }
                    ],
                    DisplayCount = 1,
                    TotalCount = 1,
                    HasMore = false
                });

            var result = await _service.GetPreferenceProperties(BuildPreferenceRequest(), _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            var listing = Assert.Single(result.Value!.Listings);
            Assert.Equal("listing-1", listing.ListingId);
            Assert.Equal("https://example.com/img.jpg", listing.ImageUrl);
            Assert.Equal("123 Main St", listing.AddressLine1);
            Assert.Equal("Sydney", listing.Suburb);
            Assert.Equal(3, listing.Bedrooms);
            Assert.Equal(2, listing.Bathrooms);
            Assert.Equal(2500m, listing.Price);
            Assert.Equal(120m, listing.BuildingSizeSqm);
            Assert.Equal("Apartment", listing.PropertyType);
        }

        [Fact]
        public async Task GetPreferenceProperties_WhenHttpRequestExceptionThrown_ShouldThrowAiServiceException()
        {
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiTenantPreferenceSearchResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network error."));

            await Assert.ThrowsAsync<AiServiceException>(() =>
                _service.GetPreferenceProperties(BuildPreferenceRequest(), _userId, CancellationToken.None));
        }

        [Fact]
        public async Task GetPreferenceProperties_WhenTaskCanceledExceptionThrown_ShouldThrowAiServiceException()
        {
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiTenantPreferenceSearchResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Request timed out."));

            await Assert.ThrowsAsync<AiServiceException>(() =>
                _service.GetPreferenceProperties(BuildPreferenceRequest(), _userId, CancellationToken.None));
        }

        [Fact]
        public async Task GetPreferenceProperties_WhenHttpRequestExceptionThrown_ShouldLogError()
        {
            SetupUserExists(true);
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiTenantPreferenceSearchResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network error."));

            await Assert.ThrowsAsync<AiServiceException>(() =>
                _service.GetPreferenceProperties(BuildPreferenceRequest(), _userId, CancellationToken.None));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(_userId.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region StreamReply

        [Fact]
        public async Task StreamReply_WithMissingUser_ShouldThrowAiServiceException()
        {
            SetupUserExists(false);

            await Assert.ThrowsAsync<AiServiceException>(async () =>
            {
                await foreach (var _ in _service.StreamReply(new ChatRequest { Message = "Hello" }, CancellationToken.None)) { }
            });
        }

        [Fact]
        public async Task StreamReply_WithValidInput_ShouldYieldChunks()
        {
            SetupUserExists(true);
            var sseBody = "data: Hello\ndata: World\ndata: [DONE]\n";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sseBody, Encoding.UTF8, "text/event-stream")
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(CreateHttpClientWithResponse(response));

            var chunks = new List<string>();
            await foreach (var chunk in _service.StreamReply(new ChatRequest { Message = "Hello" }, CancellationToken.None))
                chunks.Add(chunk);

            Assert.Equal(["Hello", "World"], chunks);
        }

        [Fact]
        public async Task StreamReply_ShouldSkipNonDataLines()
        {
            SetupUserExists(true);
            var sseBody = "event: start\ndata: Hello\n: comment\ndata: [DONE]\n";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sseBody, Encoding.UTF8, "text/event-stream")
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(CreateHttpClientWithResponse(response));

            var chunks = new List<string>();
            await foreach (var chunk in _service.StreamReply(new ChatRequest { Message = "Hello" }, CancellationToken.None))
                chunks.Add(chunk);

            Assert.Equal(["Hello"], chunks);
        }

        [Fact]
        public async Task StreamReply_WhenHttpRequestFails_ShouldThrowAiServiceException()
        {
            SetupUserExists(true);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(new MockHttpMessageHandler(new HttpRequestException("Network error."))));

            await Assert.ThrowsAsync<AiServiceException>(async () =>
            {
                await foreach (var _ in _service.StreamReply(new ChatRequest { Message = "Hello" }, CancellationToken.None)) { }
            });
        }

        [Fact]
        public async Task StreamReply_WhenServerReturnsNonSuccess_ShouldThrowHttpRequestException()
        {
            SetupUserExists(true);
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(CreateHttpClientWithResponse(response));

            await Assert.ThrowsAsync<AiServiceException>(async () =>
            {
                await foreach (var _ in _service.StreamReply(new ChatRequest { Message = "Hello" }, CancellationToken.None)) { }
            });
        }

        #endregion
    }

    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;

        public MockHttpMessageHandler(HttpResponseMessage response) => _response = response;
        public MockHttpMessageHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (_exception is not null)
                throw _exception;

            return Task.FromResult(_response!);
        }
    }
}
