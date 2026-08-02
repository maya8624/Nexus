using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nexus.Application.Common;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.Development.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection")!;

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention()
    .Options;

using var db = new AppDbContext(options);

var now = DateTimeOffset.UtcNow;
var currentHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
var todayStartUtc = new DateTimeOffset(now.Date, TimeSpan.Zero);

var fingerprints = new List<Fingerprint>();
var occurrences = new List<FingerprintOccurrence>();

// ---------------------------------------------------------------------------
// Wipe (fingerprint tables only — ingest_cursor belongs to the real pipeline)
// ---------------------------------------------------------------------------
Console.WriteLine($"Target: {new Npgsql.NpgsqlConnectionStringBuilder(connectionString).Host}");
Console.WriteLine("Wiping existing fingerprint data...");
await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE fingerprint_occurrences, fingerprints CASCADE");

// Raw App Insights severities, matching each seeded row's intended FingerprintLevel so the ids
// here are the same ones the real pipeline would derive for the same signature.
const int WarningSeverity = 2;
const int ErrorSeverity = 3;

Fingerprint AddFingerprint(
    string hash,
    FingerprintLevel level,
    string messageTemplate,
    FingerprintCategory? category = null,
    ClassificationSource? classifiedBy = null,
    string? exceptionType = null,
    string? operation = null,
    string? serviceName = null,
    bool autoFixEligible = false,
    GithubIssueStatus githubStatus = GithubIssueStatus.None,
    int? issueNumber = null,
    DateTimeOffset? issueFiledAt = null,
    DateTimeOffset? lastCommentedAt = null,
    double daysOld = 3)
{
    var firstSeen = now.AddDays(-daysOld);
    var fingerprint = new Fingerprint
    {
        Id = FingerprintHasher.GenerateFingerprintId(hash),
        Hash = hash,
        Level = level,
        Category = category,
        ClassifiedBy = classifiedBy,
        ExceptionType = exceptionType,
        MessageTemplate = messageTemplate,
        Operation = operation,
        ServiceName = serviceName,
        AutoFixEligible = autoFixEligible,
        GithubStatus = githubStatus,
        GithubIssueNumber = issueNumber,
        GithubIssueFiledAtUtc = issueFiledAt,
        GithubLastCommentedAtUtc = lastCommentedAt,
        FirstSeenUtc = firstSeen,
        LastSeenUtc = now,
        CreatedAtUtc = firstSeen,
        UpdatedAtUtc = now
    };
    fingerprints.Add(fingerprint);
    return fingerprint;
}

// Buckets are (hoursAgo, count) pairs; sparkline queries group occurrences by hour,
// so each pair becomes one bar. historicalCount pads TotalCount for old fingerprints
// whose earlier occurrence rows would already have been trimmed in real life.
void AddHistory(Fingerprint fingerprint, (int HoursAgo, int Count)[] buckets, string sampleMessage, int historicalCount = 0)
{
    foreach (var (hoursAgo, count) in buckets)
    {
        var bucketStart = currentHour.AddHours(-hoursAgo);
        occurrences.Add(new FingerprintOccurrence
        {
            FingerprintId = fingerprint.Id,
            OccurredAt = bucketStart,
            OccurrenceCount = count,
            RenderedMessage = sampleMessage,
            CreatedAtUtc = bucketStart
        });
    }

    fingerprint.TotalCount = buckets.Sum(b => b.Count) + historicalCount;
    fingerprint.LastSeenUtc = currentHour.AddHours(-buckets.Min(b => b.HoursAgo)).AddMinutes(37);
}

Console.WriteLine("Seeding mock fingerprints...");

// 1. Error / DependencyFailure / Open #101 — busy sparkline, commented 30 min ago
var fp1 = AddFingerprint(
    FingerprintHasher.ComputeExceptionHash("Nexus.Application.Services.InvoiceExtractionJob!ExecuteAsync", ErrorSeverity),
    FingerprintLevel.Error,
    "Response status code does not indicate success: {n} (Service Unavailable).",
    FingerprintCategory.DependencyFailure, ClassificationSource.Rule,
    exceptionType: "System.Net.Http.HttpRequestException",
    operation: "POST /api/internal/invoices/extract",
    serviceName: "nexus-api-dev",
    githubStatus: GithubIssueStatus.Open, issueNumber: 101,
    issueFiledAt: now.AddDays(-2), lastCommentedAt: now.AddMinutes(-30),
    daysOld: 5);
AddHistory(fp1, [(7, 4), (6, 6), (5, 3), (4, 8), (3, 5), (2, 9), (1, 7), (0, 4)],
    "Response status code does not indicate success: 503 (Service Unavailable).", historicalCount: 120);

// 2. Error / NewRegression / Open #102 — filed today, auto-fix eligible
var fp2 = AddFingerprint(
    FingerprintHasher.ComputeExceptionHash("Nexus.Application.Services.FileUploadService!ConfirmUploadAsync", ErrorSeverity),
    FingerprintLevel.Error,
    "Object reference not set to an instance of an object.",
    FingerprintCategory.NewRegression, ClassificationSource.Rule,
    exceptionType: "System.NullReferenceException",
    operation: "POST /api/files/confirm/{id}",
    serviceName: "nexus-api-dev",
    autoFixEligible: true,
    githubStatus: GithubIssueStatus.Open, issueNumber: 102,
    issueFiledAt: todayStartUtc.AddHours(2),
    daysOld: 0.3);
AddHistory(fp2, [(3, 2), (1, 3)],
    "Object reference not set to an instance of an object.");

// 3. Error / DataQuality / no issue yet — above threshold, "File issue" button enabled
var fp3 = AddFingerprint(
    FingerprintHasher.ComputeExceptionHash("Nexus.Application.Services.InvoiceService!MapExtractionResult", ErrorSeverity),
    FingerprintLevel.Error,
    "The JSON value could not be converted to System.Decimal. Path: $.total_amount.",
    FingerprintCategory.DataQuality, ClassificationSource.Llm,
    exceptionType: "System.Text.Json.JsonException",
    operation: "InvoiceExtractionJob.ExecuteAsync",
    serviceName: "nexus-api-dev",
    autoFixEligible: true,
    daysOld: 1);
AddHistory(fp3, [(5, 2), (2, 4), (0, 3)],
    "The JSON value could not be converted to System.Decimal. Path: $.total_amount | LineNumber: 0 | BytePositionInLine: 214.");

// 4. Warning / Performance / no issue — sparse sparkline
var rawPerf = "Blob upload for container 'rec-dev-invoices' took 28451 ms, exceeding the 5000 ms threshold";
var fp4 = AddFingerprint(
    FingerprintHasher.ComputeTraceHash(FingerprintHasher.NormalizeMessage(rawPerf), WarningSeverity),
    FingerprintLevel.Warning,
    FingerprintHasher.NormalizeMessage(rawPerf),
    FingerprintCategory.Performance, ClassificationSource.Rule,
    operation: "BlobStorageService.UploadAsync",
    serviceName: "nexus-api-dev",
    daysOld: 4);
AddHistory(fp4, [(6, 1), (3, 2), (0, 1)], rawPerf, historicalCount: 15);

// 5. Error / ConfigAuth / Closed #103 — resolved earlier, reopen path testable
var fp5 = AddFingerprint(
    FingerprintHasher.ComputeExceptionHash("Nexus.Network.Services.PayPalAuthService!GetAccessTokenAsync", ErrorSeverity),
    FingerprintLevel.Error,
    "Response status code does not indicate success: {n} (Unauthorized).",
    FingerprintCategory.ConfigAuth, ClassificationSource.Rule,
    exceptionType: "System.Net.Http.HttpRequestException",
    operation: "POST /api/orders/{id}/payments",
    serviceName: "nexus-api-dev",
    githubStatus: GithubIssueStatus.Closed, issueNumber: 103,
    issueFiledAt: now.AddDays(-6),
    daysOld: 7);
AddHistory(fp5, [(4, 1)],
    "Response status code does not indicate success: 401 (Unauthorized).", historicalCount: 34);

// 6. Error / RecurringKnown / Open #104 — spike-shaped sparkline
var fp6 = AddFingerprint(
    FingerprintHasher.ComputeExceptionHash("Npgsql.NpgsqlConnection!Open", ErrorSeverity),
    FingerprintLevel.Error,
    "Failed to connect to {n}.{n}.{n}.{n}:{n}",
    FingerprintCategory.RecurringKnown, ClassificationSource.Rule,
    exceptionType: "Npgsql.NpgsqlException",
    operation: "FingerprintIngestJob.ExecuteAsync",
    serviceName: "nexus-api-dev",
    githubStatus: GithubIssueStatus.Open, issueNumber: 104,
    issueFiledAt: now.AddDays(-3), lastCommentedAt: now.AddHours(-2),
    daysOld: 10);
AddHistory(fp6, [(7, 1), (6, 1), (5, 2), (4, 1), (3, 1), (2, 14), (1, 22), (0, 9)],
    "Failed to connect to 10.0.0.4:5432", historicalCount: 210);

// 7. Warning / DataQuality / Pr #105 — PR-open pill (unreachable via pipeline locally)
var rawPr = "Skipping enquiry '8f4c2d1a-93b7-4e0d-a1c5-77d2f0b3e6a9': listing has no active agent assigned";
var fp7 = AddFingerprint(
    FingerprintHasher.ComputeTraceHash(FingerprintHasher.NormalizeMessage(rawPr), WarningSeverity),
    FingerprintLevel.Warning,
    FingerprintHasher.NormalizeMessage(rawPr),
    FingerprintCategory.DataQuality, ClassificationSource.Llm,
    operation: "EnquiryService.CreateAsync",
    serviceName: "nexus-api-dev",
    autoFixEligible: true,
    githubStatus: GithubIssueStatus.Pr, issueNumber: 105,
    issueFiledAt: now.AddDays(-1.5),
    daysOld: 2);
AddHistory(fp7, [(5, 3), (4, 2), (1, 1)], rawPr, historicalCount: 8);

// 8. Error / DependencyFailure / Merged #106 — merged pill
var fp8 = AddFingerprint(
    FingerprintHasher.ComputeExceptionHash("Nexus.Application.Services.AiService!SendChatAsync", ErrorSeverity),
    FingerprintLevel.Error,
    "The operation was canceled after {n} ms waiting for a response from rec_brain.",
    FingerprintCategory.DependencyFailure, ClassificationSource.Rule,
    exceptionType: "System.Threading.Tasks.TaskCanceledException",
    operation: "POST /api/ai/chat",
    serviceName: "nexus-api-dev",
    githubStatus: GithubIssueStatus.Merged, issueNumber: 106,
    issueFiledAt: now.AddDays(-8),
    daysOld: 9);
AddHistory(fp8, [(6, 1)],
    "The operation was canceled after 30000 ms waiting for a response from rec_brain.", historicalCount: 67);

// 9. Error / unclassified — LLM fallback hasn't run yet
var fp9 = AddFingerprint(
    FingerprintHasher.ComputeExceptionHash("Nexus.Application.Services.IngestionJob!ExecuteAsync", ErrorSeverity),
    FingerprintLevel.Error,
    "Index was outside the bounds of the array.",
    exceptionType: "System.IndexOutOfRangeException",
    operation: "IngestionJob.ExecuteAsync",
    serviceName: "nexus-api-dev",
    daysOld: 0.5);
AddHistory(fp9, [(2, 2), (0, 1)], "Index was outside the bounds of the array.");

// 10. Warning / unclassified / all-null enrichment — trace-origin nulls edge case
var rawNulls = "Retrying transient failure (attempt 3 of 5)";
var fp10 = AddFingerprint(
    FingerprintHasher.ComputeTraceHash(FingerprintHasher.NormalizeMessage(rawNulls), WarningSeverity),
    FingerprintLevel.Warning,
    FingerprintHasher.NormalizeMessage(rawNulls),
    daysOld: 1.2);
AddHistory(fp10, [(4, 1), (1, 2)], rawNulls);

// 11. Error / NewRegression / brand-new — single occurrence minutes ago, near-empty sparkline
var fp11 = AddFingerprint(
    FingerprintHasher.ComputeExceptionHash("Nexus.Application.Services.GitHubIssueService!CreateIssueAsync", ErrorSeverity),
    FingerprintLevel.Error,
    "Validation Failed: field 'assignees' is invalid.",
    FingerprintCategory.NewRegression, ClassificationSource.Rule,
    exceptionType: "Octokit.ApiValidationException",
    operation: "FingerprintIngestJob.ExecuteAsync",
    serviceName: "nexus-api-dev",
    daysOld: 0.01);
AddHistory(fp11, [(0, 1)], "Validation Failed: field 'assignees' is invalid.");

// 12. Warning / ConfigAuth / Open #107 — second issue filed today (stats > 1)
var rawJwt = "JWT validation failed for request to '/api/fingerprints': token expired at 07/27/2026 04:12:55";
var fp12 = AddFingerprint(
    FingerprintHasher.ComputeTraceHash(FingerprintHasher.NormalizeMessage(rawJwt), WarningSeverity),
    FingerprintLevel.Warning,
    FingerprintHasher.NormalizeMessage(rawJwt),
    FingerprintCategory.ConfigAuth, ClassificationSource.Llm,
    operation: "JwtBearerMiddleware.Invoke",
    serviceName: "nexus-api-dev",
    githubStatus: GithubIssueStatus.Open, issueNumber: 107,
    issueFiledAt: todayStartUtc.AddHours(5), lastCommentedAt: now.AddHours(-3),
    daysOld: 6);
AddHistory(fp12, [(7, 2), (5, 1), (3, 3), (0, 2)], rawJwt, historicalCount: 44);

db.Fingerprints.AddRange(fingerprints);
db.FingerprintOccurrences.AddRange(occurrences);
await db.SaveChangesAsync();

Console.WriteLine($"Seed complete: {fingerprints.Count} fingerprints, {occurrences.Count} occurrence rows.");
Console.WriteLine();
Console.WriteLine($"{"Id",-14} {"Level",-8} {"Category",-18} {"Status",-8} {"Issue",-6} Total");
foreach (var fp in fingerprints)
    Console.WriteLine($"{fp.Id,-14} {fp.Level,-8} {fp.Category?.ToString() ?? "(none)",-18} {fp.GithubStatus,-8} {(fp.GithubIssueNumber?.ToString() ?? "-"),-6} {fp.TotalCount}");
