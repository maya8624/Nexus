using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace Nexus.Tests.Integration;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("nexus_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected NexusWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected int PropertyTypeId { get; private set; }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new NexusWebApplicationFactory(_postgres.GetConnectionString());
        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var propertyType = await db.PropertyTypes.FirstOrDefaultAsync(x => x.Name == "House")
            ?? (await db.PropertyTypes.AddAsync(new PropertyType
            {
                Name = "House",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            })).Entity;

        await db.SaveChangesAsync();

        PropertyTypeId = propertyType.Id;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected IServiceScope CreateScope() => Factory.Services.CreateScope();
}
