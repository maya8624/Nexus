using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Infrastructure.Interfaces
{
    public interface IUserRepository : IRepositoryBase<User>
    {
        Task<User?> GetByEmail(string email);
        Task<User?> GetEmailUser(string email, string password);
    }
}
