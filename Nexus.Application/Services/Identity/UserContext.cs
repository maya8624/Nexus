using Microsoft.AspNetCore.Http;
using Nexus.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Services.Identity
{
    public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
    {
        // Property to always fetch the current user
        private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        // Manual lookup instead of using FindFirstValue extension
        public string? UserId => User?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                                 ?? User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        public string? Email => User?.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                                ?? User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    }
}
