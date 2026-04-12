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

        [AllowAnonymous]
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            // TODO: remove mock — restore real implementation below
            await Task.CompletedTask;
            return Ok(new ChatResponse
            {
                Answer = "Great question! Based on current listings, we have a range of properties available across Sydney, Melbourne, and Brisbane that match your criteria. I can provide you with more details on specific properties or help you narrow down your search further. Just let me know what you're looking for!",
                ThreadId = request.ThreadId,
            });

            //var result = await _aiService.GetAnswer(request.Message, request.ThreadId, request.PropertyId, cancellationToken);

            //if (result.IsSuccess)
            //    return Ok(result.Value);

            //return MapFailure(result);
        }
    }
}
