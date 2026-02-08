using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusPay.Application.Dtos;
using NexusPay.Application.Interfaces;

namespace NexusPay.Api.Controllers
{
    public class AuthController : NexusPayControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("external-login")]
        public async Task<ActionResult<UserResponse>> ExternalLogin([FromBody] ExternalLoginRequest request)
        {
            var user = await _authService.VerifyProvider(request.Provider, request.IdToken);
            
            if (user == null) 
                return Unauthorized();

            return Ok(user);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // This tells the browser to delete the cookie by setting its expiration to the past
            _tokenService.DeleteTokenCookie();
            return Ok();
        }

        [HttpGet("me")]
        public ActionResult<UserResponse> GetMe()
        {
            var user = _tokenService.GetCurrentUser();
            return Ok(user);
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
