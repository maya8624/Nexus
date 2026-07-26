using Nexus.Application.Settings;
using Nexus.Network;
using Nexus.Network.Enums;

namespace Nexus.Application.Common
{
    public static class AiRequestOptionsFactory
    {
        public static RequestBuilderOptions Build(AiServiceSettings settings, object body, string endpoint)
        {
            return new RequestBuilderOptions
            {
                Method = HttpMethod.Post,
                AuthScheme = AuthScheme.None,
                Headers = new Dictionary<string, string>
                {
                    ["X-API-Key"] = settings.ApiKey
                },
                Body = body,
                Url = $"{settings.BaseUrl}/{endpoint}"
            };
        }

        public static RequestBuilderOptions Build(AiServiceSettings settings, HttpContent content, string endpoint)
        {
            return new RequestBuilderOptions
            {
                Method = HttpMethod.Post,
                AuthScheme = AuthScheme.None,
                Headers = new Dictionary<string, string>
                {
                    ["X-API-Key"] = settings.ApiKey
                },
                Content = content,
                Url = $"{settings.BaseUrl}/{endpoint}"
            };
        }
    }
}
