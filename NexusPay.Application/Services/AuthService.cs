using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NexusPay.Application.Dtos;
using NexusPay.Application.Exceptions;
using NexusPay.Application.Interfaces;
using NexusPay.Domain.Entities;
using NexusPay.Infrastructure.Interfaces;
using System.Security.Claims;

namespace NexusPay.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly  IUserRepository _userRepo;
        private readonly AuthSettings _settings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _uow;

        public AuthService(IUserRepository userRepo, IOptions<AuthSettings> settings, IHttpContextAccessor httpContextAccessor, IUnitOfWork uow)
        {
            _userRepo = userRepo;
            _settings = settings.Value;
            _httpContextAccessor = httpContextAccessor;
            _uow = uow;
        }

        public async Task SignIn(User user)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            // 1. Create Claims (The user's identity data)
            var claims = new List<Claim>
            {
                // Internal database ID (Critical for your PaymentService)
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name ?? "User")
            };

            // 2. Create the Identity
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 3. Configure Cookie behavior
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Remember user even after browser closes
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7), // Session length
                AllowRefresh = true // Automatically refresh the cookie during activity
            };

            // 4. Write the cookie to the Response
            // This creates the 'nexus_pay_token' we defined in your Extension class
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        public async Task<ExternalUserResponse?> VerifyProvider(string provider, string token)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    // Ensure this matches the Client ID you'll put in your React app
                    Audience = [_settings.GoogleClientId]
                };

                // Validate the token cryptographically
                var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);

                // BEST PRACTICE: Only allow login if Google has verified the email address
                if (payload == null || payload.EmailVerified == false) 
                    return null;

                return new ExternalUserResponse
                {
                    ProviderKey = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name,
                    Picture = payload.Picture
                };
            }
            catch (InvalidJwtException)
            {
                // Token is expired, malformed, or has an invalid signature
                return null;
            }
            catch (Exception)
            {
                // General error (e.g., network issues reaching Google's public keys)
                return null;
            }
        }

        //TODO: hash password
        public async Task<User> CreateEmailUser(string email, string password, string? name)
        {
            var existing = await _userRepo.GetByEmail(email);

            if (existing != null) 
                throw new UserException("Email already registered");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Name = name,
                PasswordHash = password,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            await _userRepo.Create(user);
            return user;
        }

        public async Task<User> CreateAuthUser(ExternalUserResponse externalUser, string providerName)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = externalUser.Email,
                Name = externalUser.Name,
                CreatedAt = DateTimeOffset.UtcNow,
                Logins =
                [
                    new() {
                        Id = Guid.NewGuid(),
                        Provider = providerName,
                        ProviderKey = providerName,
                        LastLoginAt = DateTimeOffset.UtcNow,
                    }
                ]
            };

            await _userRepo.Create(user);
            await _uow.SaveChanges();
            return user;
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await _userRepo.GetByEmail(email);
        }

        //public async Task<User?> GetByProviderId(string provider, string providerUserId)
        //{
        //    return await _userRepo.GetByProviderId(provider, providerUserId);
        //}

        public async Task<User?> GetEmailUser(string email, string password)
        {
            return await _userRepo.GetEmailUser(email, password);
        }
    }
}
