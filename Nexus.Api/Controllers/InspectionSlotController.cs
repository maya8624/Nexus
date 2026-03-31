using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Api.Controllers
{
    [Route("api/inspection-slots")]
    public class InspectionSlotController : AppControllerBase
    {
        private readonly IInspectionSlotService _slotService;

        public InspectionSlotController(IInspectionSlotService slotService)
        {
            _slotService = slotService;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInspectionSlotRequest request, CancellationToken ct)
        {
            var result = await _slotService.CreateAsync(request, ct);
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableSlots([FromQuery] Guid listingId, CancellationToken ct)
        {
            var result = await _slotService.GetAvailableSlotsAsync(listingId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _slotService.GetInspectionSlotByIdAsync(id, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInspectionSlotRequest request, CancellationToken ct)
        {
            var result = await _slotService.Update(id, request, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpPatch("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            var result = await _slotService.Cancel(id, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            return Ok();
        }
    }
}
