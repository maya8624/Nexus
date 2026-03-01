using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusPay.Application.Dtos;
using NexusPay.Application.Factories;
using NexusPay.Application.Interfaces;

namespace NexusPay.Api.Controllers
{
    public class AuthController : NexusPayControllerBase
    {
        private readonly IAuthServiceFactory _authFactory;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;

        public AuthController(ITokenService tokenService, IAuthServiceFactory authFactory, IUserService userService)
        {
            _tokenService = tokenService;
            _authFactory = authFactory;
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<bool>> Login([FromBody] EmailLoginRequest request)
        {
            var result = await _userService.Login(request.Email, request.Password);
            
            if (result == false)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password"
                });
            }

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserResponse>> Register([FromBody] EmailLoginRequest request)
        {
            var user = await _userService.RegisterEmailUser(request.Email, request.Password);
            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("external-login")]
        public async Task<ActionResult<UserResponse>> ExternalLogin([FromBody] ExternalLoginRequest request)
        {
            var authService = _authFactory.GetAuthProvider(request.Provider);

            var user = await authService.Authenticate(request.Provider, request.IdToken);

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
