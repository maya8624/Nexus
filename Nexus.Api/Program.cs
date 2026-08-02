using Azure.Identity;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using Nexus.Api.Extensions;
using Nexus.Api.Filters;
using Nexus.Application.Extensions;
using Nexus.Application.Interfaces.Business;
using Serilog;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Swashbuckle.AspNetCore.Filters;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUrl = builder.Configuration["KeyVaultUrl"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

// Serilog is registered as an ILoggerProvider instead of replacing the logger factory via
// UseSerilog. The OpenTelemetry provider from AddNexusTelemetry then receives log records
// straight from Microsoft.Extensions.Logging with their level and category intact. Routing
// them through Serilog's bridge instead flattened every trace to SeverityLevel 0 in App
// Insights, which the fingerprint trace query (SeverityLevel >= 2) would never match.
// Levels for both providers come from the Logging:LogLevel section; Serilog's own
// MinimumLevel still gates its Console/File sinks.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/nexus-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => 
        c.UseNpgsqlConnection(
            builder.Configuration.GetConnectionString("DefaultConnection")
        ),
         new PostgreSqlStorageOptions
        {
            SchemaName               = "hangfire",   
            PrepareSchemaIfNecessary = true,         // auto-creates hangfire schema on first run
        }
    )
);

builder.Services.AddHangfireServer();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddNexusAuth(builder.Configuration);
builder.Services.AddNexusRateLimiting(builder.Configuration);
builder.Services.AddNexusTelemetry(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexus", Version = "v1" });
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

var app = builder.Build();

// Must run before anything that reads HttpContext.Connection.RemoteIpAddress (e.g. rate limiting) —
// App Service sits in front of the app as a reverse proxy, so without this the app only ever sees
// App Service's edge IP, not the real client. ForwardLimit = 1 trusts exactly the one hop App Service
// itself appends; App Service's edge IPs aren't a fixed, enumerable set, so KnownProxies/KnownNetworks
// isn't usable here.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor,
    ForwardLimit = 1
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthFilter()]
});

RecurringJob.AddOrUpdate<ISasExpiryJob>(
    "sas-expiry-check",
    job => job.ExecuteAsync(),
    "*/15 * * * *");

// Registered only when a workspace is configured (Phase 7 wiring); otherwise the job
// would fail every run against a blank WorkspaceId. RemoveIfExists keeps a previously
// registered job from firing after the setting is blanked, since Hangfire persists
// recurring jobs in storage.
if (!string.IsNullOrEmpty(builder.Configuration["FingerprintIngestSettings:WorkspaceId"]))
{
    RecurringJob.AddOrUpdate<IFingerprintIngestJob>(
        "fingerprint-ingest",
        job => job.ExecuteAsync(),
        "*/15 * * * *");
}
else
{
    RecurringJob.RemoveIfExists("fingerprint-ingest");
}

app.MapControllers();


try
{
    app.Run();
}
catch (Exception ex)
{
    // Serilog is only an ILoggerProvider now (see the logging setup above), so a static Log.Fatal
    // call bypasses Microsoft.Extensions.Logging and never reaches the OpenTelemetry exporter —
    // a startup crash, arguably the most important exception the process can produce, would be
    // invisible to App Insights and so unfingerprintable. Logging through the container reaches
    // both Serilog's sinks and OpenTelemetry; fall back to the static logger if the container
    // itself is what failed.
    try
    {
        app.Services
            .GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>()
            .LogCritical(ex, "Application failed to start.");
    }
    catch
    {
        Log.Fatal(ex, "Application failed to start.");
    }
}
finally
{
    // Disposing the host shuts down the OpenTelemetry providers, which flushes buffered telemetry.
    // Without it the exception logged above can be lost on the way out.
    await app.DisposeAsync();
    Log.CloseAndFlush();
}

public partial class Program { }
