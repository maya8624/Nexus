using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Application.Interfaces.Business;

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
        public async Task<ActionResult<PropertyListResponse>> GetProperties([FromQuery] PropertyQueryRequest request, CancellationToken ct = default)
        {            
            var result = await _propertyService.GetProperties(request, ct);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PropertyDto>> GetProperty(Guid id, CancellationToken ct)
        {
            var result = await _propertyService.GetPropertyById(id, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
