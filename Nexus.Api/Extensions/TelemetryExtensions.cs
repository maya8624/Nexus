using Azure.Monitor.OpenTelemetry.AspNetCore;
using Npgsql;
using OpenTelemetry.Resources;

namespace Nexus.Api.Extensions
{
    public static class TelemetryExtensions
    {
        // Becomes AppRoleName/cloud_RoleName in Azure Monitor. The fingerprint ingest KQL groups
        // exceptions by it, so it must stay stable and distinct from the Functions app's role name.
        private const string ServiceName = "nexus-api";

        public static IServiceCollection AddNexusTelemetry(this IServiceCollection services, IConfiguration config)
        {
            // A blank/absent connection string (local dev before Azure setup) must not crash boot —
            // UseAzureMonitor throws at startup without one. Same skip convention as the blank
            // GitHubSettings token and the FingerprintIngestSettings:WorkspaceId job guard.
            var connectionString = config["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            if (string.IsNullOrWhiteSpace(connectionString))
                return services;

            services.AddOpenTelemetry()
                .UseAzureMonitor(options => options.ConnectionString = connectionString)
                .ConfigureResource(resource => resource.AddService(serviceName: ServiceName))
                .WithTracing(tracing => tracing.AddNpgsql());

            return services;
        }
    }
}
