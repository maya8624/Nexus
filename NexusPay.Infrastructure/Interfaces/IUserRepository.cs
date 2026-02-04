using NexusPay.Domain.Entities;
using NexusPay.Infrastructure.Persistence;
using NexusPay.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusPay.Infrastructure.Interfaces
{
    public interface IUserRepository : IRepositoryBase<User>
    {
        Task<User?> GetByEmail(string email);
        Task<User?> GetEmailUser(string email, string password);
    }
}
