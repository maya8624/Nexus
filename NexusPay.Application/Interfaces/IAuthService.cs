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
        Task<User> CreateAuthUser(ExternalUserResponse externalUser, string provider);
        //Task<User?> GetByProviderId(string provider, string providerUserId);
        Task<User> CreateEmailUser(string email, string password, string? name);
        Task<User?> GetEmailUser(string email, string password);
        Task<ExternalUserResponse?> VerifyProvider(string provider, string token);
        Task SignIn(User user);
    }
}
