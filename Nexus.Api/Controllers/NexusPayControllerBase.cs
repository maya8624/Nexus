using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Nexus.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NexusPayControllerBase : ControllerBase
    {
    }
}
