using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Nexus.Functions.Functions
{
    public class BlobIngestionFunction
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BlobIngestionFunction> _logger;

        public BlobIngestionFunction(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<BlobIngestionFunction> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [Function(nameof(BlobIngestionFunction))]
        public async Task Run(
            [BlobTrigger("%IngestionContainerName%/{blobName}", Connection = "BlobStorageConnectionString")] Stream blobStream,
            string blobName,
            FunctionContext context)
        {
            var containerName = _configuration["IngestionContainerName"]!;
            var apiUrl = _configuration["NexusApiUrl"]!;
            var apiKey = _configuration["NexusApiKey"]!;

            _logger.LogInformation("Blob ingestion triggered for {BlobName} in {Container}.", blobName, containerName);

            var payload = new { BlobName = blobName, ContainerName = containerName };

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            var response = await http.PostAsJsonAsync($"{apiUrl}/api/internal/documents/ingest", payload);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ingestion API call failed for {BlobName}. Status: {Status}. Body: {Body}",
                    blobName, response.StatusCode, body);

                throw new InvalidOperationException($"Ingestion API returned {response.StatusCode} for blob {blobName}.");
            }

            _logger.LogInformation("Ingestion job enqueued for {BlobName}.", blobName);
        }
    }
}
