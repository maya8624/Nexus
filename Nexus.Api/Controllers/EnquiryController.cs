using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Api.Controllers
{
    [Route("api/enquiries")]
    public class EnquiryController : AppControllerBase
    {
        private readonly IEnquiryService _enquiryService;

        public EnquiryController(IEnquiryService enquiryService)
        {
            _enquiryService = enquiryService;
        }

        [HttpPost]
        public async Task<ActionResult<EnquiryResponse>> Create([FromBody] CreateEnquiryRequest request, CancellationToken ct)
        {
            var result = await _enquiryService.CreateAsync(request, UserId, ct);
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);

            return MapFailure(result);
        }

        [HttpGet("my")]
        public async Task<ActionResult<IReadOnlyList<EnquiryResponse>>> GetMyEnquiries(CancellationToken ct)
        {
            var result = await _enquiryService.GetMyEnquiriesAsync(UserId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<EnquiryResponse>> GetById(Guid id, CancellationToken ct)
        {
            var result = await _enquiryService.GetByIdAsync(id, UserId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        // [HttpPatch("{id:guid}")]
        // public async Task<ActionResult<EnquiryResponse>> Update(Guid id, [FromBody] UpdateEnquiryRequest request, CancellationToken ct)
        // {
        //     var result = await _enquiryService.UpdateAsync(id, request, UserId, ct);
        //     if (result.IsSuccess)
        //         return Ok(result.Value);

        //     return MapFailure(result);
        // }
    }
}
