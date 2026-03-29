using Nexus.Application.Dtos;
using Nexus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Application.Interfaces
{
    public interface IAuthService
    {
        string ProviderName { get; }
        Task<ExternalUserResponse?> Authenticate(string provider, string token);
    }
}
