using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUserRepository _userRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _uow;
        private readonly JwtSettings _jwtSettings;

        public UserService(
            IUserRepository userRepo,
            IRefreshTokenRepository refreshTokenRepo,
            IUnitOfWork uow,
            IPasswordHasherService passwordHasher,
            ITokenService tokenService,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepo = userRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _uow = uow;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
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
            var (rawRefreshToken, refreshEntity) = CreateRefreshTokenEntity(user.Id);

            await _refreshTokenRepo.Create(refreshEntity, cancellationToken);
            await _uow.SaveChanges();
            
            var accessToken = _tokenService.CreateToken(user.Id.ToString(), user.Email, user.FirstName, user.LastName);

            return Result<UserResponse>.Success(new UserResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Token = accessToken,
                RefreshToken = rawRefreshToken,
            });
        }

        public async Task<Result<UserResponse>> Login(string email, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByEmail(email);

            if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash!, password))
                return Result<UserResponse>.Unauthorized("INVALID_CREDENTIALS", "Invalid email or password");

            var (rawRefreshToken, refreshEntity) = CreateRefreshTokenEntity(user.Id);

            await _refreshTokenRepo.Create(refreshEntity, cancellationToken);
            await _uow.SaveChanges();
            
            var accessToken = _tokenService.CreateToken(user.Id.ToString(), email, user.FirstName, user.LastName);

            return Result<UserResponse>.Success(new UserResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Token = accessToken,
                RefreshToken = rawRefreshToken,
            });
        }

        public async Task<Result<UserResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var tokenHash = _tokenService.HashToken(refreshToken);
            var existing = await _refreshTokenRepo.GetByTokenHash(tokenHash, cancellationToken);

            if (existing == null || existing.IsRevoked || existing.ExpiresAt <= DateTimeOffset.UtcNow)
                return Result<UserResponse>.Unauthorized("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired");

            existing.IsRevoked = true;

            var user = existing.User;
            var accessToken = _tokenService.CreateToken(user.Id.ToString(), user.Email, user.FirstName, user.LastName);
            var (newRaw, newEntity) = CreateRefreshTokenEntity(user.Id);
            
            await _refreshTokenRepo.Create(newEntity, cancellationToken);
            await _uow.SaveChanges();

            return Result<UserResponse>.Success(new UserResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Token = accessToken,
                RefreshToken = newRaw,
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

        private (string raw, RefreshToken entity) CreateRefreshTokenEntity(Guid userId)
        {
            var raw = _tokenService.GenerateRefreshToken();
            var entity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = _tokenService.HashToken(raw),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                CreatedAt = DateTimeOffset.UtcNow,
                IsRevoked = false,
            };
            return (raw, entity);
        }
    }
}
