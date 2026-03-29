using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Api.Controllers
{
    public class BookingController : AppControllerBase
    {
        private readonly IInspectionBookingService _bookingService;

        public BookingController(IInspectionBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<InspectionBookingDto>> GetInspectionBooking(Guid id, CancellationToken ct)
        {
            var result = await _bookingService.GetInspectionBookingByIdAsync(id, ct);
            if (result.IsSuccess && result.Value is not null)
            {
                return Ok(result.Value);
            }

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpGet("availability")]
        public async Task<ActionResult<InspectionAvailabilityResponse>> CheckInspectionAvailability(
            [FromQuery] CheckInspectionAvailabilityRequest request,
            CancellationToken ct)
        {
            var result = await _bookingService.CheckInspectionAvailabilityAsync(request, ct);
            if (result.IsSuccess && result.Value is not null)
            {
                return Ok(result.Value);
            }

            return MapFailure(result);
        }


        [AllowAnonymous]
        [HttpPost("bookings")]
        public async Task<ActionResult<InspectionBookingDto>> CreateInspectionBooking([FromBody] CreateInspectionBookingRequest request, CancellationToken ct)
        {
            var result = await _bookingService.CreateInspectionBookingAsync(request, ct);

            if (result.IsSuccess && result.Value is not null)
            {
                var rte = CreatedAtAction(nameof(GetInspectionBooking), new { id = result.Value.Id }, result.Value);
            }

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpPost("{id:guid}/cancel")]
        public async Task<ActionResult<InspectionBookingDto>> CancelInspectionBooking(Guid id, CancellationToken ct)
        {
            var result = await _bookingService.CancelInspectionBookingAsync(id, ct);

            if (result.IsSuccess && result.Value is not null)
                return Ok(result.Value);

            return MapFailure(result);
        }
    }
}
