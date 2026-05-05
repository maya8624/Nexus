using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Nexus.Application.Settings;
using System.Text;

namespace Nexus.Api.Extensions
{
    public static class AuthExtensions
    {
        //TODO: make sure strong types options are used
        public static IServiceCollection AddNexusAuth(this IServiceCollection services, IConfiguration config)
        {
            services.AddAuthentication(options =>
            {
                // Set JWT as the boss for both Identifying and Challenging (401 errors)
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!)),

                    ValidateIssuer = true,
                    ValidIssuer = config["JwtSettings:Issuer"],

                    ValidateAudience = true,
                    ValidAudience = config["JwtSettings:Audience"],

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Removes the 5-minute "grace period" for tighter security
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogError("JWT authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogInformation("JWT token validated for {User}", context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}
