using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using System.Security.Claims;

namespace Nexus.Api.Controllers
{
    public class AiController : AppControllerBase
    {
        private readonly IAiService _aiService;
        private readonly ILogger<AiController> _logger;

        public AiController(IAiService aiService, ILogger<AiController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }


        // private readonly IMediator _mediator;

        // public ChatController(IMediator mediator, ILogger<ChatController> logger)
        // {
        //     _mediator = mediator;
        //     _logger = logger;
        // }
        /// <summary>
        /// Send a message to the AI agent and receive a response.
        /// React sends: { message, session_id }
        /// .NET adds: userId from JWT
        /// Python receives: { message, session_id (scoped to user) }
        /// </summary>

        [AllowAnonymous]
        [HttpPost("chat")]
        public async Task<ActionResult<ChatResponse>> GetAnswer([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            var response = await _aiService.GetAnswer(request.Message, request.SessionId, cancellationToken);
            return response;

            //     // Extract userId from JWT claims — set by your auth middleware
            //     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //     if (string.IsNullOrEmpty(userId))
            //     {
            //         return Unauthorized("User ID not found in token.");
            //     }

            //     _logger.LogInformation(
            //         "Chat request from user {UserId}, session {SessionId}",
            //         userId,
            //         request.SessionId);

            //     var command = new SendMessageCommand
            //     {
            //         Message = request.Message,
            //         SessionId = request.SessionId,
            //         UserId = userId,
            //     };

            //     try
            //     {
            //         var response = await _mediator.Send(command, cancellationToken);
            //         return Ok(response);
            //     }
            //     catch (AiServiceException ex)
            //     {
            //         _logger.LogWarning(ex, "AI service error for session {SessionId}", request.SessionId);
            //         return StatusCode(
            //             StatusCodes.Status503ServiceUnavailable,
            //             new { error = ex.Message });
            //     }
        }
    }
}
