using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Common;
using Nexus.Application.Dtos;

namespace Nexus.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public class AppControllerBase : ControllerBase
    {
        protected ObjectResult MapFailure<T>(Result<T> result)
        {
            var statusCode = result.Status switch
            {
                ResultStatus.ValidationError => StatusCodes.Status400BadRequest,
                ResultStatus.NotFound => StatusCodes.Status404NotFound,
                ResultStatus.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
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
