using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Network;
using Nexus.Network.Enums;
using Nexus.Network.Interfaces;
using Nexus.Network.Services;

namespace Nexus.Application.Services
{
    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<PayPalAuthService> _logger;
        private readonly AiSidecarSettings _settings;
        public FraudDetectionService(IHttpClientService httpClientService, IOptions<AiSidecarSettings> settings, ILogger<PayPalAuthService> logger)
        {
            _httpClientService = httpClientService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<FraudPredictionResponse> CheckTransaction(FraudPredictionRequest request)
        {
            try
            {
                var headers = new Dictionary<string, string>
                {
                    [ "Api-Key" ] = _settings.ApiKey,
                };

                var options = new RequestBuilderOptions
                {
                    Method = HttpMethod.Post,
                    AuthScheme = AuthScheme.None,
                    Headers = headers,
                    Body = request,
                    Url = _settings.Url
                };

                var message = HttpRequestFactory.CreateHttpRequestMessage(options);
                var result = await _httpClientService.ExecuteRequest<FraudPredictionResponse>(message);

                if (result.IsFraud && result.ConfidenceScore > 0.85)
                    _logger.LogWarning("Fraudulent activity detected. Score: {Score}", result.ConfidenceScore);

                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reach AI Sidecar at {Url}", _settings.Url);

                return new FraudPredictionResponse
                {
                    IsFraud = true,
                    ConfidenceScore = 1.0,
                    AgentDecision = "SYSTEM_UNAVAILABLE_FAIL_SAFE"
                };

                // Emergency Fallback for SaaS
                //return new FraudPredictionResponse
                //{
                //    IsFraud = false, // Let it through
                //    ConfidenceScore = 0.0,
                //    AgentDecision = "BYPASS_SYSTEM_OFFLINE" // Mark for manual audit later
                //};
            }
        }
    }
}
