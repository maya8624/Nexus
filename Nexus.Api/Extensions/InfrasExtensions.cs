using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nexus.Application.Settings;
using Nexus.Application.Interfaces;
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
            services.Configure<BlobStorageSettings>(config.GetSection(nameof(BlobStorageSettings)));
            services.Configure<FingerprintIngestSettings>(config.GetSection(nameof(FingerprintIngestSettings)));
            services.Configure<FingerprintRoutingSettings>(config.GetSection(nameof(FingerprintRoutingSettings)));
            services.Configure<GitHubSettings>(config.GetSection(nameof(GitHubSettings)));
            services.AddSingleton(sp =>
                new BlobServiceClient(sp.GetRequiredService<IOptions<BlobStorageSettings>>().Value.ConnectionString));
            services.AddSingleton(sp => new LogsQueryClient(new DefaultAzureCredential()));
            services.AddSingleton<Octokit.IGitHubClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<GitHubSettings>>().Value;
                var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("Nexus"));
                // Blank placeholder token (local dev before setup) must not crash app boot -
                // Octokit's Credentials ctor rejects empty strings.
                if (!string.IsNullOrWhiteSpace(settings.Token))
                    client.Credentials = new Octokit.Credentials(settings.Token);
                return client;
            });
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
                        .AllowAnyMethod();

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
