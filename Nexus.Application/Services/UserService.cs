using Nexus.Application.Dtos;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Domain.Entities;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUserRepository _userRepo;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _uow;

        public UserService(
            IUserRepository userRepo, 
            IUnitOfWork uow, 
            IPasswordHasherService passwordHasher, 
            ITokenService tokenService)
        {
            _userRepo = userRepo;
            _uow = uow;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<UserResponse> RegisterEmailUser(string email, string password)
        {
            var existing = await _userRepo.GetByEmail(email);

            if (existing != null)
                throw new UserException("Email already registered");

            var hashedPassword = _passwordHasher.HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hashedPassword,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            await _userRepo.Create(user, CancellationToken.None);

            return new UserResponse
            {
                Email = user.Email,
                UserId = user.Id.ToString(), // TODO: need to return????
            };
        }

        public async Task<User> CreateAuthUser(ExternalUserResponse externalUser, string providerName)
        {
            var existing = await _userRepo.GetByEmail(externalUser.Email);

            if (existing != null)
                return existing;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = externalUser.Email,
                FirstName = externalUser.Name,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Logins =
                [
                    new() {
                        Id = Guid.NewGuid(),
                        Provider = providerName,
                        ProviderKey = externalUser.ProviderKey,
                        LastLoginAt = DateTimeOffset.UtcNow,
                    }
                ]
            };

            await _userRepo.Create(user, CancellationToken.None);
            await _uow.SaveChanges();
            return user;
        }

        public async Task<bool> Login(string email, string password)
        {
            var user = await _userRepo.GetByEmail(email);

            if (user == null)
                return false;

            var isVerified = _passwordHasher.VerifyPassword(user.PasswordHash, password);

            if (isVerified == false)
                return false;

            // Generate OUR token (Doesn't matter which provider they used)
            var jwtToken = _tokenService.CreateToken(user.Id.ToString(), email);

            return true;
        }

        //public async Task<User?> GetByProviderId(string provider, string providerUserId)
        //{
        //    return await _userRepo.GetByProviderId(provider, providerUserId);
        //}   
    }
}
