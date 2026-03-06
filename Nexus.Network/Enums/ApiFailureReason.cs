using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Network.Enums
{
    public enum ApiFailureReason
    {
        Unauthorized,
        Forbidden,
        NotFound,
        ValidationError,
        RateLimited,
        ServerError,
        Timeout,
        Unknown
    }
}
