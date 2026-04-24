using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;

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

        public async Task<Result<UserResponse>> RegisterEmailUser(string email, string password, string? firstName = null, string? lastName = null, CancellationToken cancellationToken = default)
        {
            var existing = await _userRepo.GetByEmail(email);

            if (existing != null)
                return Result<UserResponse>.Conflict("EMAIL_TAKEN", "Email already registered");

            var hashedPassword = _passwordHasher.HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hashedPassword,
                FirstName = firstName,
                LastName = lastName,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                IsActive = true
            };

            await _userRepo.Create(user, cancellationToken);
            await _uow.SaveChanges();
            _tokenService.CreateToken(user.Id.ToString(), user.Email, user.FirstName, user.LastName);

            return Result<UserResponse>.Success(new UserResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
            });
        }

        public async Task<User> CreateAuthUser(ExternalUserResponse externalUser, string providerName, CancellationToken cancellationToken = default)
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

            await _userRepo.Create(user, cancellationToken);
            await _uow.SaveChanges();
            return user;
        }

        public async Task<Result<UserResponse>> Login(string email, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByEmail(email);

            if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, password))
                return Result<UserResponse>.Unauthorized("INVALID_CREDENTIALS", "Invalid email or password");

            _tokenService.CreateToken(user.Id.ToString(), email, user.FirstName, user.LastName);

            return Result<UserResponse>.Success(new UserResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
            });
        }

        //public async Task<User?> GetByProviderId(string provider, string providerUserId)
        //{
        //    return await _userRepo.GetByProviderId(provider, providerUserId);
        //}   
    }
}
