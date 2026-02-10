using Azure.Core;
using Microsoft.AspNetCore.Http;
using NexusPay.Application.Dtos;
using NexusPay.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusPay.Application.Interfaces
{
    public interface IAuthService
    {
        string ProviderName { get; }
        Task<ExternalUserResponse?> Authenticate(string provider, string token);
    }
}
