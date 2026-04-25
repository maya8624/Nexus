using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Filters;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Api.Controllers
{
    [Route("api/internal")]
    [ServiceFilter(typeof(InternalApiKeyFilter))]
    public class InternalController : ControllerBase
    {
        private readonly IInspectionSlotService _slotService;
        private readonly IInspectionBookingService _bookingService;


        public InternalController(IInspectionSlotService slotService, IInspectionBookingService bookingService)
        {
            _slotService = slotService;
            _bookingService = bookingService;
        }

        [HttpGet("inspection-bookings/available/{propertyId:guid}")]
        public async Task<IActionResult> GetAvailableSlots([FromRoute] Guid propertyId, CancellationToken ct)
        {
            var result = await _slotService.GetAvailableSlotsAsync(propertyId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }

        [HttpPost("inspection-bookings")]
        public async Task<IActionResult> Book([FromBody] InternalInspectionBookingRequest request, CancellationToken ct)
        {
            var result = await _bookingService.CreateAsyncForInternal(request, ct);
            if (result.IsSuccess)
                return StatusCode(201, result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }

        [HttpPatch("inspection-bookings/{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelBookingRequest request, CancellationToken ct)
        {
            var result = await _bookingService.CancelAsync(id, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }
    }
}