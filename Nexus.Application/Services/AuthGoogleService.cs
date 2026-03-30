using Google.Apis.Auth;
using Microsoft.Extensions.Options;
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

        public async Task<ExternalUserResponse?> Authenticate(string provider, string token)
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

                var externalUser = new ExternalUserResponse
                {
                    ProviderKey = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name,
                    Picture = payload.Picture
                };

                // Find/Create user and link the provider in SQL
                var user = await _userService.CreateAuthUser(externalUser, provider);
                
                // Generate OUR token (Doesn't matter which provider they used)
                var jwtToken = _tokenService.CreateToken(externalUser.ProviderKey, externalUser.Email);

                return externalUser;
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
    }
}
