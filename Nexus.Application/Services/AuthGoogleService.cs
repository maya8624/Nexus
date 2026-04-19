using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Settings;

namespace Nexus.Application.Services
{
    public class AuthGoogleService : IAuthService
    {
        public string ProviderName => "google";
        private readonly AuthSettings _settings;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;

        public AuthGoogleService(IOptions<AuthSettings> settings, ITokenService tokenService, IUserService userService)
        {
            _settings = settings.Value;
            _tokenService = tokenService;
            _userService = userService;
        }

        public async Task<Result<UserResponse>> Authenticate(string provider, string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = [_settings.GoogleClientId]
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);

                if (payload == null || payload.EmailVerified == false)
                    return Result<UserResponse>.Unauthorized("GOOGLE_AUTH_FAILED", "Google token is invalid or email is not verified");

                var externalUser = new ExternalUserResponse
                {
                    ProviderKey = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name,
                    Picture = payload.Picture
                };

                var user = await _userService.CreateAuthUser(externalUser, provider, cancellationToken);

                _tokenService.CreateToken(user.Id.ToString(), user.Email, user.FirstName, user.LastName);

                return Result<UserResponse>.Success(new UserResponse
                {
                    UserId = user.Id.ToString(),
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                });
            }
            catch (InvalidJwtException)
            {
                return Result<UserResponse>.Unauthorized("GOOGLE_TOKEN_INVALID", "Google token is expired or has an invalid signature");
            }
            catch (Exception)
            {
                return Result<UserResponse>.Unauthorized("GOOGLE_AUTH_ERROR", "An error occurred while authenticating with Google");
            }
        }
    }
}
