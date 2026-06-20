using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Functions.Functions;
using Nexus.Functions.Settings;
using Nexus.Tests.Unit.Helpers;
using System.Net;
using Xunit;

namespace Nexus.Tests.Unit.Functions
{
    [Trait("Category", "Unit")]
    public class BlobIngestionFunctionTests
    {
        private readonly Mock<ILogger<BlobIngestionFunction>> _loggerMock = new();
        private readonly BlobIngestionSettings _settings = new()
        {
            IngestionContainerName = "rec-dev-ingestion",
            InvoiceContainerName = "rec-dev-invoices",
            NexusApiUrl = "https://localhost:7289",
            NexusApiKey = "test-api-key"
        };

        private BlobIngestionFunction CreateSut(HttpResponseMessage response)
        {
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return new BlobIngestionFunction(
                factoryMock.Object,
                Options.Create(_settings),
                _loggerMock.Object);
        }

        [Fact]
        public async Task Run_WhenApiReturnsSuccess_ShouldLogEnqueued()
        {
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK));

            await sut.Run(Stream.Null, "test.pdf", null!);

            _loggerMock.VerifyLog(LogLevel.Information, "Ingestion job enqueued for test.pdf");
        }

        [Fact]
        public async Task Run_WhenApiReturnsFailure_ShouldLogErrorAndThrow()
        {
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("upstream error")
            });

            var act = () => sut.Run(Stream.Null, "test.pdf", null!);

            await Assert.ThrowsAsync<InvalidOperationException>(act);
            _loggerMock.VerifyLog(LogLevel.Error, "Ingestion API call failed for test.pdf");
        }
    }

    file sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}
