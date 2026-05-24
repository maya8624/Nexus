using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Filters;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces.Business;
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
        private readonly IDocumentSuggestionService _documentSuggestionService;

        public InternalController(
            IInspectionSlotService slotService,
            IInspectionBookingService bookingService,
            IDepositService depositService,
            IDocumentSuggestionService documentSuggestionService)
        {
            _slotService = slotService;
            _bookingService = bookingService;
            _depositService = depositService;
            _documentSuggestionService = documentSuggestionService;
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
        public async Task<IActionResult> GetBookingById(Guid id, [FromBody] AiInspectionBookingRequest request, CancellationToken ct)
        {
            var result = await _bookingService.GetByIdAsync(id, request.UserId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }

        [HttpPatch("inspection-bookings/{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] AiInspectionBookingRequest request, CancellationToken ct)
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

        [HttpPost("document-suggestions")]
        public async Task<IActionResult> SaveDocumentSuggestion([FromBody] SaveDocumentSuggestionRequest request, CancellationToken ct)
        {
            var result = await _documentSuggestionService.SaveAsync(request, ct);
            if (result.IsSuccess)
                return StatusCode(201, result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }

        [HttpGet("document-suggestions/{docId:guid}/user/{userId:guid}")]
        public async Task<IActionResult> GetDocumentSuggestion(Guid docId, Guid userId, CancellationToken ct)
        {
            var result = await _documentSuggestionService.GetByDocIdAsync(docId, userId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors.FirstOrDefault());
        }
    }
}