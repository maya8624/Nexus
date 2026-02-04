using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusPay.Application.Dtos;
using NexusPay.Application.Interfaces;
using NexusPay.Application.Services;
using NexusPay.Domain.Entities;
using System.Security.Claims;

namespace NexusPay.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }

        [HttpPost("external-login")]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request)
        {
            var externalUser = await _authService.VerifyProvider(request.Provider, request.IdToken);
            
            if (externalUser == null) 
                return Unauthorized();

            // TODO: move to AuthService
            // 2. Find/Create user and link the provider in SQL
            var user = await _authService.CreateAuthUser(externalUser, request.Provider);

            // TODO: move to AuthService
            // 3. Issue the 'nexus_pay_token' cookie
            //await _authService.SignIn(user);
            // 2. Generate OUR token (Doesn't matter which provider they used)
            var jwtToken = _tokenService.CreateToken(externalUser.ProviderKey, externalUser.Email);

            // TODO: bring the cookie name from appsettings
            Response.Cookies.Append("__Host-NexusPay-Auth", jwtToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            // TODO: create a dto
            return Ok(new { user.Email, user.Name });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // This tells the browser to delete the cookie by setting its expiration to the past
            Response.Cookies.Delete("nexus_pay_token");
            return Ok();
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetMe()
        {
            // The middleware populated this from the cookie!
            // "sub" now matches JwtRegisteredClaimNames.Sub from your CreateToken method
            var userId = User.FindFirstValue("sub");
            var email = User.FindFirstValue(ClaimTypes.Email);

            return Ok(new { userId, email });
        }

        //[HttpPost("register")]
        //public async Task<IActionResult> Register(RegisterDto dto)
        //{
        //    // 1. Create User in DB
        //    // 2. Generate JWT
        //    // 3. Append HttpOnly Cookie to Response
        //    // 4. Return User object (id, name, email, role)
        //}
    }
}
