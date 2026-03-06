using Microsoft.Extensions.Logging;
using Nexus.Network.Interfaces;
using System.Net.Http.Json;

namespace Nexus.Network.Services
{
    public class HttpClientService : IHttpClientService
    {
        private readonly IHttpClientFactory _httpClient;
        private readonly ILogger<PayPalAuthService> _logger;

        public HttpClientService(IHttpClientFactory httpClientFactory, ILogger<PayPalAuthService> logger)
        {
            _httpClient = httpClientFactory;
            _logger = logger;
        }

        public async Task<T> ExecuteRequest<T>(HttpRequestMessage request)//, CancellationToken? cancellationToken)
        {
            try
            {
                using var http = _httpClient.CreateClient();
                var response = await http.SendAsync(request);

                if (response.IsSuccessStatusCode == false)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    var failureReason = HttpStatusFailureMap.Resolve(response.StatusCode);

                    _logger.LogWarning(
                        "HTTP request failed with {StatusCode} mapped to {FailureReason}",
                        response.StatusCode,
                        errorBody);

                    throw new HttpRequestException($"HTTP request failed with {response.StatusCode} mapped to {failureReason}");
                }

                var content = await response.Content.ReadFromJsonAsync<T>();
                if (content == null)
                {
                    _logger.LogError("Empty response body.");
                     throw new HttpRequestException("Empty response body");
                }

                return content;
            }
            catch (TaskCanceledException ex)
            {
                throw new TaskCanceledException("Request timed out", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException("Network error occurred", ex);
            }
        }

        public Task ExecuteRequest(HttpRequestMessage request, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
