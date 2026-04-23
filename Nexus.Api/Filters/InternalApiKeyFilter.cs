using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Nexus.Application.Settings;

namespace Nexus.Api.Filters
{

    public class InternalApiKeyFilter : IActionFilter
    {
        private readonly AiServiceSettings _settings;

        public InternalApiKeyFilter(IOptions<AiServiceSettings> settings)
        {
            _settings = settings.Value;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Request.Headers.TryGetValue("X-Internal-Api-Key", out var key);

            if (key != _settings.ApiKey)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

    }
}