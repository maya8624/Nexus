using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Api.Controllers
{
    [Route("api/stats")]
    public class FingerprintStatsController : AppControllerBase
    {
        private readonly IFingerprintService _fingerprintService;

        public FingerprintStatsController(IFingerprintService fingerprintService)
        {
            _fingerprintService = fingerprintService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(FingerprintStatsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<FingerprintStatsResponse>> GetStats(CancellationToken ct)
        {
            var result = await _fingerprintService.GetStatsAsync(ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }
    }
}
