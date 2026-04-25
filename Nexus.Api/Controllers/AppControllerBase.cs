using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Responses;
using System.Security.Claims;

namespace Nexus.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class AppControllerBase : ControllerBase
    {
        protected Guid UserId
        {
            get
            {
                var sub = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                          ?? HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                Guid.TryParse(sub, out var id);
                return id;
            }
        }

        protected ObjectResult MapFailure<T>(Result<T> result)
        {
            var statusCode = result.Status switch
            {
                ResultStatus.ValidationError => StatusCodes.Status400BadRequest,
                ResultStatus.NotFound        => StatusCodes.Status404NotFound,
                ResultStatus.Conflict        => StatusCodes.Status409Conflict,
                ResultStatus.Unauthorized    => StatusCodes.Status401Unauthorized,
                ResultStatus.Forbidden       => StatusCodes.Status403Forbidden,
                _                            => throw new InvalidOperationException($"Unhandled ResultStatus: {result.Status}")
            };

            var firstError = result.Errors.FirstOrDefault();
            var response = new ErrorResponse
            {
                Code = statusCode,
                Name = firstError?.Code ?? "RequestFailed",
                Message = result.Errors.Count == 0
                    ? "The request failed."
                    : string.Join(" | ", result.Errors.Select(x => x.Message))
            };

            return StatusCode(statusCode, response);
        }
    }
}
