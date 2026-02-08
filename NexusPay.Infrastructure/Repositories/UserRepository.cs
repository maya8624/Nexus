using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using NexusPay.Domain.Entities;
using NexusPay.Infrastructure.Interfaces;
using NexusPay.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusPay.Infrastructure.Repositories
{
    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        private readonly NexusPayContext _context;

        public UserRepository(NexusPayContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<User?> GetEmailUser(string email, string password)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email && x.PasswordHash == password);
        }
    }
}
