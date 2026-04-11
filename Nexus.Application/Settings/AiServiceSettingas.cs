using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Settings
{
    /// <summary>
    /// Typed settings for the Python AI service.
    /// Bound from appsettings.json → "AiService" section.
    /// </summary>
    public class AiServiceSettings
    {
        public required string BaseUrl { get; init; }
        public required string ApiKey { get; init; }
        public int TimeoutSeconds { get; init; } = 30;
    }
}
