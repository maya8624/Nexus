using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace NexusPay.Api.Extensions
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),

                    ValidateIssuer = true,
                    ValidIssuer = config["Jwt:Issuer"],

                    ValidateAudience = true,
                    ValidAudience = config["Jwt:Audience"],

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Removes the 5-minute "grace period" for tighter security
                };

                // Find the token in the code
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Look for the cookie we set in the login method
                        var token = context.Request.Cookies[config["Jwt:CookieName"]!];

                        if (string.IsNullOrEmpty(token) == false)
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}
