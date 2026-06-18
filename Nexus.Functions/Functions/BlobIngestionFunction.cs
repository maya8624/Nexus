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
            [BlobTrigger("%IngestionContainerName%/{blobName}", Connection = "BlobStorageConnectionString")] Stream blobStream,
            string blobName,
            FunctionContext context)
        {
            var containerName = _settings.IngestionContainerName;
            var apiUrl = _settings.NexusApiUrl;

            _logger.LogInformation("Blob ingestion triggered for {BlobName} in {Container}.", blobName, containerName);

            var payload = new { BlobName = blobName, ContainerName = containerName };

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("X-Api-Key", _settings.NexusApiKey);

            var response = await http.PostAsJsonAsync($"{apiUrl}/api/internal/documents/ingest", payload);

            if (response.IsSuccessStatusCode == false)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ingestion API call failed for {BlobName}. Status: {Status}. Body: {Body}", blobName, response.StatusCode, body);

                throw new InvalidOperationException($"Ingestion API returned {response.StatusCode} for blob {blobName}.");
            }

            _logger.LogInformation("Ingestion job enqueued for {BlobName}.", blobName);
        }
    }
}
