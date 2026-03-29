using Nexus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IUserRepository : IRepositoryBase<User>
    {
        Task<User?> GetByEmail(string email);
        Task<User?> GetEmailUser(string email, string password);
    }
}
