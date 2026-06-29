using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Functions.Settings;
using System.Net.Http.Json;

namespace Nexus.Functions.Functions
{
    public class BlobIngestionFunction
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BlobIngestionSettings _settings;
        private readonly ILogger<BlobIngestionFunction> _logger;

        public BlobIngestionFunction(
            IHttpClientFactory httpClientFactory,
            IOptions<BlobIngestionSettings> settings,
            ILogger<BlobIngestionFunction> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        [Function(nameof(BlobIngestionFunction))]
        public async Task Run(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            FunctionContext context)
        {
            var subject = eventGridEvent.Subject;
            var blobsMarker = "/blobs/";
            var markerIndex = subject.IndexOf(blobsMarker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                _logger.LogWarning("Unexpected subject format: {Subject}", subject);
                return;
            }

            var blobName = subject[(markerIndex + blobsMarker.Length)..];
            var containerName = _settings.IngestionContainerName;

            _logger.LogInformation("Blob ingestion triggered for {BlobName} in {Container}.", blobName, containerName);

            var payload = new { BlobName = blobName, ContainerName = containerName };

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("X-Api-Key", _settings.NexusApiKey);

            var response = await http.PostAsJsonAsync($"{_settings.NexusApiUrl}/api/internal/documents/ingest", payload);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ingestion API call failed for {BlobName}. Status: {Status}. Body: {Body}", blobName, response.StatusCode, body);
                throw new InvalidOperationException($"Ingestion API returned {response.StatusCode} for blob {blobName}.");
            }

            _logger.LogInformation("Ingestion job enqueued for {BlobName}.", blobName);
        }
    }
}
