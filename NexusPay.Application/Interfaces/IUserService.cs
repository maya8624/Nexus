using NexusPay.Application.Dtos;
using NexusPay.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusPay.Application.Interfaces
{
    public interface IUserService
    {
        Task<User> CreateAuthUser(ExternalUserResponse externalUser, string provider);
        Task<User> CreateEmailUser(string email, string password, string? name);
        Task<User?> GetEmailUser(string email, string password);
    }
}
