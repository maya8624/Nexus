using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;
using Nexus.Domain.Enums;

namespace Nexus.Api.Controllers
{
    [Route("api/fingerprints")]
    public class FingerprintController : AppControllerBase
    {
        private readonly IFingerprintService _fingerprintService;

        public FingerprintController(IFingerprintService fingerprintService)
        {
            _fingerprintService = fingerprintService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IList<FingerprintListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IList<FingerprintListItemResponse>>> GetList(
            [FromQuery] GithubIssueStatus? status, [FromQuery] FingerprintLevel? level, CancellationToken ct)
        {
            var result = await _fingerprintService.GetListAsync(status, level, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FingerprintDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FingerprintDetailResponse>> GetById(string id, CancellationToken ct)
        {
            var result = await _fingerprintService.GetByIdAsync(id, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpPost("{id}/file-issue")]
        [ProducesResponseType(typeof(FingerprintDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<FingerprintDetailResponse>> FileIssue(string id, CancellationToken ct)
        {
            var result = await _fingerprintService.FileIssueAsync(id, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpPost("{id}/send-to-agent")]
        [ProducesResponseType(typeof(FingerprintDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<FingerprintDetailResponse>> SendToAgent(string id, CancellationToken ct)
        {
            var result = await _fingerprintService.SendToAgentAsync(id, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpPost("{id}/resolve")]
        [ProducesResponseType(typeof(FingerprintDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<FingerprintDetailResponse>> Resolve(string id, CancellationToken ct)
        {
            var result = await _fingerprintService.ResolveAsync(id, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }
    }
}
