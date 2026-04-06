using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexus.Infrastructure.Persistence;
using Nexus.Tests.Integration.Helpers;

namespace Nexus.Tests.Integration;

public class NexusWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public NexusWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["JwtSettings:Key"]        = JwtTokenHelper.TestJwtKey,
                ["JwtSettings:Issuer"]     = JwtTokenHelper.TestIssuer,
                ["JwtSettings:Audience"]   = JwtTokenHelper.TestAudience,
                ["JwtSettings:CookieName"] = "__Host-Nexus-Auth",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_connectionString)
                       .UseSnakeCaseNamingConvention());
        });
    }
}
