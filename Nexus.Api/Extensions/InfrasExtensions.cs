using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Settings;
using Nexus.Infrastructure.Persistence;
using Nexus.Network;
using Stripe;
using Stripe.Checkout;

namespace Nexus.Api.Extensions
{
    public static class InfrasExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {

            services.Configure<AiServiceSettings>(config.GetSection(nameof(AiServiceSettings)));
            services.Configure<AuthSettings>(config.GetSection(nameof(AuthSettings)));
            services.Configure<JwtSettings>(config.GetSection(nameof(JwtSettings)));
            services.Configure<PayPalSettings>(config.GetSection(nameof(PayPalSettings)));
            services.Configure<StripeSettings>(config.GetSection(nameof(StripeSettings)));
            services.Configure<SmtpSettings>(config.GetSection(nameof(SmtpSettings)));

            services.AddSingleton<IStripeClient>(new StripeClient(config.GetSection(nameof(StripeSettings.SecretKey)).Value));
            services.AddScoped(x => new SessionService(x.GetRequiredService<IStripeClient>()));

            var allowedOrigins = config.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();

                    });
            });

            //services.AddDbContext<AppDbContext>(options 
            //    => options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("DefaultConnection"))
                       .UseSnakeCaseNamingConvention());

            return services;
        }
    }
}
