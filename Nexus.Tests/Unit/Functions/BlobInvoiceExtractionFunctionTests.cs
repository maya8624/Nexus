using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Functions.Functions;
using Nexus.Functions.Settings;
using Nexus.Tests.Unit.Helpers;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Nexus.Tests.Unit.Functions
{
    [Trait("Category", "Unit")]
    public class BlobInvoiceExtractionFunctionTests
    {
        private readonly Mock<ILogger<BlobInvoiceExtractionFunction>> _loggerMock = new();
        private readonly BlobIngestionSettings _settings = new()
        {
            IngestionContainerName = "rec-dev-ingestion",
            InvoiceContainerName   = "rec-dev-invoices",
            NexusApiUrl            = "https://localhost:7289",
            NexusApiKey            = "test-api-key"
        };

        private BlobInvoiceExtractionFunction CreateSut(HttpResponseMessage response)
        {
            var factory = CreateFactory(new InvoiceFakeHttpMessageHandler(response));
            return new BlobInvoiceExtractionFunction(factory, Options.Create(_settings), _loggerMock.Object);
        }

        private BlobInvoiceExtractionFunction CreateCapturingSut(out List<HttpRequestMessage> captured)
        {
            var requests = new List<HttpRequestMessage>();
            captured = requests;
            var factory = CreateFactory(new InvoiceCapturingHttpMessageHandler(requests, new HttpResponseMessage(HttpStatusCode.OK)));
            return new BlobInvoiceExtractionFunction(factory, Options.Create(_settings), _loggerMock.Object);
        }

        private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
        {
            var mock = new Mock<IHttpClientFactory>();
            mock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler));
            return mock.Object;
        }

        [Fact]
        public async Task Run_WhenApiReturnsSuccess_ShouldLogEnqueued()
        {
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.Accepted));

            await sut.Run(Stream.Null, "invoice.pdf", null!);

            _loggerMock.VerifyLog(LogLevel.Information, "Invoice extraction job enqueued for invoice.pdf");
        }

        [Fact]
        public async Task Run_WhenApiReturnsFailure_ShouldLogErrorAndThrow()
        {
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("upstream error")
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Run(Stream.Null, "invoice.pdf", null!));

            _loggerMock.VerifyLog(LogLevel.Error, "Invoice extraction API call failed for invoice.pdf");
        }

        [Fact]
        public async Task Run_WhenApiReturnsNotFound_ShouldThrow()
        {
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Run(Stream.Null, "invoice.pdf", null!));
        }

        [Fact]
        public async Task Run_ShouldPostToCorrectEndpoint()
        {
            var sut = CreateCapturingSut(out var captured);

            await sut.Run(Stream.Null, "invoice.pdf", null!);

            Assert.Single(captured);
            Assert.Equal(HttpMethod.Post, captured[0].Method);
            Assert.Equal($"{_settings.NexusApiUrl}/api/internal/invoices/extract", captured[0].RequestUri!.ToString());
        }

        [Fact]
        public async Task Run_ShouldSendApiKeyHeader()
        {
            var sut = CreateCapturingSut(out var captured);

            await sut.Run(Stream.Null, "invoice.pdf", null!);

            Assert.True(captured[0].Headers.Contains("X-Api-Key"));
            Assert.Equal("test-api-key", captured[0].Headers.GetValues("X-Api-Key").Single());
        }

        [Fact]
        public async Task Run_ShouldSendBlobNameInPayload()
        {
            var sut = CreateCapturingSut(out var captured);

            await sut.Run(Stream.Null, "invoices/abc.pdf", null!);

            var json = await captured[0].Content!.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            Assert.Equal("invoices/abc.pdf", doc.RootElement.GetProperty("BlobName").GetString());
        }

        [Fact]
        public async Task Run_ShouldLogTriggerOnStart()
        {
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.Accepted));

            await sut.Run(Stream.Null, "invoice.pdf", null!);

            _loggerMock.VerifyLog(LogLevel.Information, "invoice.pdf");
        }
    }

    file sealed class InvoiceFakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    file sealed class InvoiceCapturingHttpMessageHandler(List<HttpRequestMessage> captured, HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            captured.Add(request);
            return Task.FromResult(response);
        }
    }
}
