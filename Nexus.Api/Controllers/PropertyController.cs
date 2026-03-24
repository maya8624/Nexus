using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Interfaces;

namespace Nexus.Api.Controllers
{
    public class PropertyController : AppControllerBase
    {
        private readonly IPropertyService _propertyService;

        public PropertyController(IPropertyService propertyService)
        {
            _propertyService = propertyService;
        }

        [AllowAnonymous]
        [HttpGet("properties")]
        public async Task<ActionResult<PropertyListResponse>> GetProperties(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? type = null,
            CancellationToken ct = default)
        {
            var result = await _propertyService.GetProperties(page, pageSize, type, ct);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PropertyDto>> GetProperty(Guid id, CancellationToken ct)
        {
            var result = await _propertyService.GetPropertyById(id, ct);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("bookings")]
        public async Task<ActionResult<InspectionBookingDto>> CreateInspectionBooking(
            [FromBody] CreateInspectionBookingRequest request,
            CancellationToken ct)
        {
            var result = await _propertyService.CreateInspectionBookingAsync(request, ct);
            if (result.IsSuccess && result.Value is not null)
            {
                return CreatedAtAction(nameof(GetInspectionBooking), new { id = result.Value.Id }, result.Value);
            }

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpPost("bookings/{id:guid}/cancel")]
        public async Task<ActionResult<InspectionBookingDto>> CancelInspectionBooking(Guid id, CancellationToken ct)
        {
            var result = await _propertyService.CancelInspectionBookingAsync(id, ct);
            if (result.IsSuccess && result.Value is not null)
            {
                return Ok(result.Value);
            }

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpGet("bookings/{id:guid}")]
        public async Task<ActionResult<InspectionBookingDto>> GetInspectionBooking(Guid id, CancellationToken ct)
        {
            var result = await _propertyService.GetInspectionBookingByIdAsync(id, ct);
            if (result.IsSuccess && result.Value is not null)
            {
                return Ok(result.Value);
            }

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpGet("bookings/availability")]
        public async Task<ActionResult<InspectionAvailabilityResponse>> CheckInspectionAvailability(
            [FromQuery] CheckInspectionAvailabilityRequest request,
            CancellationToken ct)
        {
            var result = await _propertyService.CheckInspectionAvailabilityAsync(request, ct);
            if (result.IsSuccess && result.Value is not null)
            {
                return Ok(result.Value);
            }

            return MapFailure(result);
        }

        private ObjectResult MapFailure<T>(Result<T> result)
        {
            var statusCode = result.Status switch
            {
                ResultStatus.ValidationError => StatusCodes.Status400BadRequest,
                ResultStatus.NotFound => StatusCodes.Status404NotFound,
                ResultStatus.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            var firstError = result.Errors.FirstOrDefault();
            var response = new ErrorResponse
            {
                Code = statusCode,
                Name = firstError?.Code ?? "RequestFailed",
                Message = result.Errors.Count == 0
                    ? "The request failed."
                    : string.Join(" | ", result.Errors.Select(x => x.Message))
            };

            return StatusCode(statusCode, response);
        }
    }
}
