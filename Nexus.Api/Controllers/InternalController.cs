using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Filters;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Nexus.Api.Controllers
{
    [Route("api/internal")]
    [ServiceFilter(typeof(InternalApiKeyFilter))]
    public class InternalController : ControllerBase
    {
        private readonly IInspectionSlotService _slotService;
        private readonly IInspectionBookingService _bookingService;
        private readonly IDepositService _depositService;

        public InternalController(IInspectionSlotService slotService, IInspectionBookingService bookingService, IDepositService depositService)
        {
            _slotService = slotService;
            _bookingService = bookingService;
            _depositService = depositService;
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
            var bookingRequest = new InspectionBookingRequest
            {
                InspectionSlotId = request.InspectionSlotId,
                Notes = request.Notes
            };

            var result = await _bookingService.CreateAsync(bookingRequest, request.UserId, ct);
            if (result.IsSuccess)
                return StatusCode(201, result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }

        [HttpGet("inspection-bookings/my/{userId:guid}")]
        public async Task<IActionResult> GetMyBookings(Guid userId, CancellationToken ct)
        {
            var result = await _bookingService.GetMyBookingsAsync(userId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }

        [HttpGet("inspection-bookings/{id:guid}")]
        public async Task<IActionResult> GetBookingById(Guid id, [FromBody] AIInspectionBookingRequest request, CancellationToken ct)
        {
            var result = await _bookingService.GetByIdAsync(id, request.UserId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }

        [HttpPatch("inspection-bookings/{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] AIInspectionBookingRequest request, CancellationToken ct)
        {
            var result = await _bookingService.CancelAsync(id, request.UserId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }

        [HttpGet("deposit/my/{listingId:guid}/{userId:guid}")]
        public async Task<IActionResult> GetMyDeposit(Guid listingId, Guid userId, CancellationToken ct)
        {
            var result = await _depositService.GetMyDeposit(listingId, userId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }
    }
}