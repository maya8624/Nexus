using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Api.Controllers
{
    [Route("api/inspection-bookings")]
    public class InspectionBookingController : AppControllerBase
    {
        private readonly IInspectionBookingService _bookingService;

        public InspectionBookingController(IInspectionBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        public async Task<ActionResult<InspectionBookingDto>> Create([FromBody] CreateInspectionBookingRequest request, CancellationToken ct)
        {
            var result = await _bookingService.CreateAsync(request, ct);
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);

            return MapFailure(result);
        }

        [HttpGet("my")]
        public async Task<ActionResult<IReadOnlyList<InspectionBookingDto>>> GetMyBookings(CancellationToken ct)
        {
            var result = await _bookingService.GetMyBookingsAsync(ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<InspectionBookingDto>> GetById(Guid id, CancellationToken ct)
        {
            var result = await _bookingService.GetByIdAsync(id, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpPatch("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            var result = await _bookingService.CancelAsync(id, ct);
            if (result.IsSuccess)
                return NoContent();

            return MapFailure(result);
        }
    }
}
