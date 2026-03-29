using Microsoft.EntityFrameworkCore;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;
using Nexus.Application.Interfaces.Repository;

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
