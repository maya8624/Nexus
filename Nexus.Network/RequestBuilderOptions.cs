using Nexus.Network.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Network
{
    public class RequestBuilderOptions
    {
        public string AuthToken { get; init; }
        public AuthScheme AuthScheme { get; set; }
        public object? Body { get; init; }
        public HttpContent Content { get; set; }
        public IDictionary<string, string>? Headers { get; init; }
        public HttpMethod Method { get; init; }
        public string Url { get; init; } = default!;
    }
}
