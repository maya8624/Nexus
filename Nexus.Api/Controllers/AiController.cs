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

        [HttpPost("copilot")]
        public async Task<IActionResult> Copilot([FromBody] CopilotRequest request, CancellationToken cancellationToken)
        {
            var result = await _aiService.GetReply(request, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpPost("copilot/stream")]
        public async Task StreamCopilot([FromBody] CopilotRequest request, CancellationToken cancellationToken)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            await foreach (var chunk in _aiService.StreamReply(request, cancellationToken))
            {
                await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }

        [HttpPost("preferences")]
        public async Task<IActionResult> Preferences(TenantPreferenceRequest request, CancellationToken ct)
        {
            var result = await _aiService.GetPreferenceProperties(request, UserId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpPost("suburb-summary")]
        public async Task<IActionResult> SuburbSummary([FromBody] SuburbSummaryRequest request, CancellationToken ct)
        {
            var result = await _aiService.GetSuburbSummary(request, UserId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpPost("enquiry-draft")]
        public async Task<IActionResult> EnquiryDraft([FromBody] EnquiryDraftRequest request, CancellationToken ct)
        {
            var result = await _aiService.GetEnquiryDraft(request, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }
    }
}
