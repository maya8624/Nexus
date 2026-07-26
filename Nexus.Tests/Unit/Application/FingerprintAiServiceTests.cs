using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;
using Nexus.Application.Services;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Network.Interfaces;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class FingerprintAiServiceTests
    {
        private readonly Mock<IHttpClientService> _httpClientServiceMock = new();
        private readonly Mock<ILogger<FingerprintAiService>> _loggerMock = new();
        private readonly FingerprintAiService _service;

        public FingerprintAiServiceTests()
        {
            var settings = Options.Create(new AiServiceSettings
            {
                BaseUrl        = "http://localhost:8000",
                ApiKey         = "test-key",
                Chat           = "api/chat",
                ChatStream     = "api/chat/stream",
                Preferences    = "api/preferences",
                SuburbSummary  = "api/suburb-summary",
                EnquiryDraft   = "api/enquiry/draft",
                Ingestion      = "api/ingest",
                InvoiceExtract = "api/documents/invoice-extract",
                Classify       = "api/fingerprints/classify",
                Summarize      = "api/fingerprints/summarize"
            });

            _service = new FingerprintAiService(_httpClientServiceMock.Object, settings, _loggerMock.Object);
        }

        private static Fingerprint BuildFingerprint() => new()
        {
            Id = "fp_deadbeef",
            Hash = "deadbeef",
            Level = FingerprintLevel.Error,
            ExceptionType = "System.Net.Http.HttpRequestException",
            MessageTemplate = "Connection refused to {n}.{n}.{n}.{n}:{n}",
            Operation = "RagService.QueryDocuments",
            GithubStatus = GithubIssueStatus.None,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        [Fact]
        public async Task ClassifyAsync_Success_MapsWireCategoryAndSetsSourceToLlm()
        {
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiFingerprintClassifyResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiFingerprintClassifyResponse
                {
                    category = "DEPENDENCY_FAILURE",
                    confidence = 0.87,
                    reason = "Outbound HTTP timeout to an external service dependency."
                });

            var result = await _service.ClassifyAsync(BuildFingerprint(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(FingerprintCategory.DependencyFailure, result.Value!.Category);
            Assert.Equal(ClassificationSource.Llm, result.Value.Source);
            Assert.Equal(0.87, result.Value.Confidence);
        }

        [Fact]
        public async Task ClassifyAsync_WhenCategoryUnrecognized_ThrowsFingerprintAiServiceException()
        {
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiFingerprintClassifyResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiFingerprintClassifyResponse
                {
                    category = "NOT_A_REAL_CATEGORY",
                    confidence = 0.5,
                    reason = "n/a"
                });

            await Assert.ThrowsAsync<FingerprintAiServiceException>(
                () => _service.ClassifyAsync(BuildFingerprint(), CancellationToken.None));
        }

        [Fact]
        public async Task ClassifyAsync_WhenHttpClientThrows_ThrowsFingerprintAiServiceException()
        {
            _httpClientServiceMock
                .Setup(x => x.ExecuteRequest<AiFingerprintClassifyResponse>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("network error"));

            await Assert.ThrowsAsync<FingerprintAiServiceException>(
                () => _service.ClassifyAsync(BuildFingerprint(), CancellationToken.None));
        }
    }
}
