using Nexus.Network.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Network
{
    public static class HttpStatusFailureMap
    {
        private static readonly IReadOnlyDictionary<HttpStatusCode, ApiFailureReason> _map
            = new Dictionary<HttpStatusCode, ApiFailureReason>
            {
                [HttpStatusCode.BadRequest] = ApiFailureReason.ValidationError,
                [HttpStatusCode.Unauthorized] = ApiFailureReason.Unauthorized,
                [HttpStatusCode.Forbidden] = ApiFailureReason.Forbidden,
                [HttpStatusCode.NotFound] = ApiFailureReason.NotFound,
                [HttpStatusCode.TooManyRequests] = ApiFailureReason.RateLimited,
                [HttpStatusCode.RequestTimeout] = ApiFailureReason.Timeout
            };

        public static ApiFailureReason Resolve(HttpStatusCode statusCode)
        {
            if (_map.TryGetValue(statusCode, out var reason))
                return reason;

            if ((int)statusCode >= 500)
                return ApiFailureReason.ServerError;

            return ApiFailureReason.Unknown;
        }
    }    
}
