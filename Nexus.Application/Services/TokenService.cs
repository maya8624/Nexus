using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Application.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nexus.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserContext _userContext;

        public TokenService(IHttpContextAccessor httpContextAccessor, IOptions<JwtSettings> settings, IUserContext userContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _jwtSettings = settings.Value;
            _userContext = userContext;
        }

        public string CreateToken(string userId, string email, string? firstName = null, string? lastName = null)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            if (firstName != null) claims.Add(new(JwtRegisteredClaimNames.GivenName, firstName));
            if (lastName != null)  claims.Add(new(JwtRegisteredClaimNames.FamilyName, lastName));

            // 2. Setup the Key and Credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            // 3. Create the Token Descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                SigningCredentials = creds,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience
            };

            // 4. Generate the String
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var token = tokenHandler.WriteToken(securityToken);

            //TODO: Set Refresh Token

            return token;
        }

        public UserResponse? GetCurrentUser()
        {
            if (_userContext == null || _userContext.IsAuthenticated == false)
                return null;

            return new UserResponse
            {
                UserId = _userContext.UserId,
                Email = _userContext.Email,
                FirstName = _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.GivenName),
                LastName = _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.FamilyName),
            };
        }

    }
}
