using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Application.Dtos
{
    public class ExternalUserResponse
    {
        public string ProviderKey { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }
}
