using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using Nexus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Interfaces.Business
{
    public interface IUserService
    {
        Task<User> CreateAuthUser(ExternalUserResponse externalUser, string provider);
        Task<UserResponse> RegisterEmailUser(string email, string password);
        Task<bool> Login(string email, string password);
    }
}
