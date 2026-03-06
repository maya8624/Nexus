using Nexus.Network.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nexus.Network
{
    public static class HttpRequestFactory
    {
        public static HttpRequestMessage CreateHttpRequestMessage(RequestBuilderOptions options)
        {
            var request = new HttpRequestMessage(options.Method, options.Url);

            if (options.AuthScheme == AuthScheme.Basic)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(AuthScheme.Basic.ToString(), options.AuthToken);
            }
            else if (options.AuthScheme == AuthScheme.Bearer && string.IsNullOrWhiteSpace(options.AuthToken) == false)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(AuthScheme.Bearer.ToString(), options.AuthToken);
            }
            else
            {
                request.Headers.Authorization = null;
            }

            if (options.Headers != null)
            {
                foreach (var header in options.Headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (options.Body != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(options.Body), Encoding.UTF8, "application/json");
            }
            else if (options.Content != null)
            {
                request.Content = options.Content;
            }


            return request;
        }
    }
}
