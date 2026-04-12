using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Nexus.Tests.Integration.Api;

public class AiControllerIntegrationTests : IntegrationTestBase
{
    private const string Endpoint = "/api/ai/chat";

    private HttpClient CreateClientWith(Mock<IAiService> mock)
    {
        return Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAiService>();
                services.AddScoped<IAiService>(_ => mock.Object);
            });
        }).CreateClient();
    }

    #region POST /api/ai/chat

    [Fact]
    public async Task Chat_WithValidRequest_Returns200AndChatResponse()
    {
        // Arrange
        var aiServiceMock = new Mock<IAiService>();
        aiServiceMock
            .Setup(x => x.GetAnswer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatResponse>.Success(new ChatResponse
            {
                Answer = "Here are some properties for you.",
                ThreadId = "session-abc"
            }));

        var client = CreateClientWith(aiServiceMock);
        var request = new ChatRequest { Message = "Show me listings", ThreadId = "session-abc" };

        // Act
        var response = await client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Answer.Should().Be("Here are some properties for you.");
        body.ThreadId.Should().Be("session-abc");
    }

    [Fact]
    public async Task Chat_WithMissingUser_Returns404()
    {
        // Arrange
        var aiServiceMock = new Mock<IAiService>();
        aiServiceMock
            .Setup(x => x.GetAnswer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatResponse>.NotFound("UserNotFound", "User not found or inactive."));

        var client = CreateClientWith(aiServiceMock);
        var request = new ChatRequest { Message = "Hello", ThreadId = "session-abc" };

        // Act
        var response = await client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task Chat_WithEmptyMessage_Returns400()
    {
        // Arrange
        var client = CreateClientWith(new Mock<IAiService>());
        var request = new ChatRequest { Message = "", ThreadId = "session-abc" };

        // Act
        var response = await client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Chat_WithMessageExceedingMaxLength_Returns400()
    {
        // Arrange
        var client = CreateClientWith(new Mock<IAiService>());
        var request = new ChatRequest { Message = new string('x', 1001), ThreadId = "session-abc" };

        // Act
        var response = await client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Chat_WhenAiServiceThrows_ReturnsErrorResponse()
    {
        // Arrange
        var aiServiceMock = new Mock<IAiService>();
        aiServiceMock
            .Setup(x => x.GetAnswer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AiServiceException("The AI service is currently unavailable. Please try again later."));

        var client = CreateClientWith(aiServiceMock);
        var request = new ChatRequest { Message = "Hello", ThreadId = "session-abc" };

        // Act
        var response = await client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error.Should().NotBeNull();
        error!.Name.Should().Be("AI_SERVICE_ERROR");
        error.Message.Should().Be("The AI service is currently unavailable. Please try again later.");
    }

    #endregion
}
