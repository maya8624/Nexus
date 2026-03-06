using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Exceptions;
using Nexus.Network.Enums;
using Nexus.Network.Extensions;
using Nexus.Network.Interfaces;
using Nexus.Network.Responses;

namespace Nexus.Network.Services
{
    public class PayPalAuthService : IPayPalAuthService
    {
        private readonly IHttpClientService _httClientService;
        private readonly ILogger<PayPalAuthService> _logger;
        private readonly PayPalSettings _settings;

        public PayPalAuthService(IHttpClientService httClientService, ILogger<PayPalAuthService> logger, IOptions<PayPalSettings> settings)
        {
            _httClientService = httClientService;
            _logger = logger;
            _settings = settings.Value;
        }

        //TODO: cache a token and reuse it until expires?
        public async Task<string> GetAccessToken()
        {            
            var options = BuildTokenRequest();
            var request = HttpRequestFactory.CreateHttpRequestMessage(options);

            var response = await _httClientService.ExecuteRequest<PayPalTokenResponse>(request);
            var token = response?.AccessToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("Failed to obtain PayPal access token.");

                throw new PayPalException("PayPal token response was invalid or missing access token.");
            }
            
            return token;
        }

        private RequestBuilderOptions BuildTokenRequest()
        {
            var url = _settings.SandboxBaseUrl.CombineUrl(_settings.SandboxTokenUrl);
            var idempotencyKey = Guid.NewGuid().ToString();

            var byteArray = Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.Secret}");
            var auth = Convert.ToBase64String(byteArray);
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            var options = new RequestBuilderOptions
            {
                AuthScheme = AuthScheme.Basic,
                AuthToken = auth,
                Method = HttpMethod.Post,
                Content = content,
                Url = url,
            };

            return options;
        }
    }
}
