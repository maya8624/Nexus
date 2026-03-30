using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Api.Controllers
{
    [Route("api/properties")]
    public class PropertyController : AppControllerBase
    {
        private readonly IPropertyService _propertyService;

        public PropertyController(IPropertyService propertyService)
        {
            _propertyService = propertyService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PropertyListResponse>> GetProperties([FromQuery] PropertyQueryRequest request, CancellationToken ct)
        {
            var result = await _propertyService.GetPropertiesAsync(request, ct);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PropertyDto>> GetById(Guid id, CancellationToken ct)
        {
            var result = await _propertyService.GetByIdAsync(id, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }
    }
}
