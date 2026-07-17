using Microsoft.AspNetCore.RateLimiting;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Settings;
using System.Threading.RateLimiting;

namespace Nexus.Api.Extensions
{
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddNexusRateLimiting(this IServiceCollection services, IConfiguration config)
        {
            var settings = config.GetSection(nameof(RateLimitSettings)).Get<RateLimitSettings>() ?? new RateLimitSettings();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("login", httpContext => GetLimiter(httpContext, settings.Login));
                options.AddPolicy("register", httpContext => GetLimiter(httpContext, settings.Register));
                options.AddPolicy("refresh", httpContext => GetLimiter(httpContext, settings.Refresh));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse
                    {
                        Code = StatusCodes.Status429TooManyRequests,
                        Name = "TooManyRequests",
                        Message = "Too many requests. Please try again later."
                    }, cancellationToken);
                };
            });

            return services;
        }

        private static RateLimitPartition<string> GetLimiter(HttpContext httpContext, RateLimitPolicySettings policy)
        {
            var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromMinutes(policy.WindowMinutes),
                SegmentsPerWindow = policy.SegmentsPerWindow,
                QueueLimit = 0
            });
        }
    }
}
