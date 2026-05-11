using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Factories;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Api.Controllers
{
    public class AuthController : AppControllerBase
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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<UserResponse>> Login([FromBody] EmailLoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.Login(request.Email, request.Password, cancellationToken);
            if (!result.IsSuccess)
                return MapFailure(result);

            return Ok(result.Value);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.RegisterEmailUser(request.Email, request.Password, request.FirstName, request.LastName, cancellationToken);
            if (!result.IsSuccess)
                return MapFailure(result);

            return StatusCode(201, result.Value);
        }

        [AllowAnonymous]
        [HttpPost("external-login")]
        public async Task<ActionResult<UserResponse>> ExternalLogin([FromBody] ExternalLoginRequest request, CancellationToken cancellationToken)
        {
            var authService = _authFactory.GetAuthProvider(request.Provider);
            var result = await authService.Authenticate(request.Provider, request.IdToken, cancellationToken);
            if (!result.IsSuccess)
                return MapFailure(result);

            return Ok(result.Value);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<UserResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.RefreshAsync(request.RefreshToken, cancellationToken);
            if (!result.IsSuccess)
                return MapFailure(result);

            return Ok(result.Value);
        }

        [HttpGet("me")]
        public ActionResult<UserResponse> GetMe()
        {
            var user = _tokenService.GetCurrentUser();
            if (user == null)
                return Unauthorized();

            return Ok(user);
        }
    }
}
