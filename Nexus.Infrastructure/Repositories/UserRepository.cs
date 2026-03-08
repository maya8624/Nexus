using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Interfaces;
using Nexus.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Infrastructure.Repositories
{
    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context) : base(context)
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
