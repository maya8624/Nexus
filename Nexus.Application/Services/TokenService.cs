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
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = creds,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience
            };

            // 4. Generate the String
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var token = tokenHandler.WriteToken(securityToken);

            CreateJwtCookie(token);
            //TODO: Set Refresh Token

            return token;
        }

        public void DeleteTokenCookie()
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(_jwtSettings.CookieName);
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

        private void CreateJwtCookie(string token)
        {
            // SameSite=None is required when frontend (http) and backend (https) run on different schemes
            // in local dev — modern browsers treat http://localhost and https://localhost as cross-site.
            // SameSite=None requires Secure=true, so both flags are tied together.
            var isHttps = _httpContextAccessor.HttpContext?.Request.IsHttps ?? false;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            };

            _httpContextAccessor.HttpContext?.Response.Cookies.Append(_jwtSettings.CookieName, token, cookieOptions);
        }
    }
}
