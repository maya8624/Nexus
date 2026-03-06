using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nexus.Network.Responses
{
    public class PayPalTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = default!;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = default!;

        [JsonPropertyName("app_id")]
        public string AppId { get; set; } = default!;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = default!;

        [JsonPropertyName("nonce")]
        public string Nonce { get; set; } = default!;
    }
}
