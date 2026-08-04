using Moq;
using Nexus.Application.Common;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class FingerprinterServiceTests
    {
        private readonly Mock<IFingerprintRepository> _repositoryMock = new();
        private readonly Mock<IFingerprintOccurrenceRepository> _occurrenceRepositoryMock = new();
        private readonly Mock<IFingerprintClassifier> _classifierMock = new();
        private readonly FingerprinterService _service;

        public FingerprinterServiceTests()
        {
            _service = new FingerprinterService(_repositoryMock.Object, _occurrenceRepositoryMock.Object, _classifierMock.Object);
        }

        // Raw App Insights severities: 2 = Warning, 3 = Error, 4 = Critical.
        private const int WarningSeverity = 2;
        private const int ErrorSeverity = 3;
        private const int CriticalSeverity = 4;

        private static AppInsightsExceptionGroupReadModel BuildExceptionRow(
            string problemId = "Nexus.Application.Services.InvoiceExtractionJob!ExecuteAsync",
            string exceptionType = "System.Net.Http.HttpRequestException",
            int count = 14,
            int severity = ErrorSeverity) => new()
        {
            ProblemId = problemId,
            ExceptionType = exceptionType,
            Severity = severity,
            Operation = "POST /api/internal/invoices/extract",
            ServiceName = "nexus-api-dev",
            SampleMessage = "Response status code does not indicate success: 503 (Service Unavailable).",
            Count = count,
            LastSeen = DateTimeOffset.Parse("2026-07-21T09:52:11Z")
        };

        private static AppInsightsTraceGroupReadModel BuildTraceRow(
            string rawMessage = "Connection refused to 10.0.0.4:5432",
            int count = 12,
            int severity = WarningSeverity) => new()
        {
            RawMessage = rawMessage,
            Severity = severity,
            Operation = "RagService.QueryDocuments",
            ServiceName = "rag-service",
            Count = count,
            LastSeen = DateTimeOffset.Parse("2026-07-16T08:45:00Z")
        };

        [Fact]
        public async Task ProcessExceptionGroupAsync_WhenHashNotFound_CreatesNewFingerprint()
        {
            var row = BuildExceptionRow();
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var (fingerprint, isNew, windowCount) = await _service.ProcessExceptionGroupAsync(row, row.LastSeen, CancellationToken.None);

            Assert.True(isNew);
            Assert.Equal(row.Count, windowCount);
            Assert.StartsWith("fp_", fingerprint.Id);
            Assert.Equal(FingerprintLevel.Error, fingerprint.Level);
            Assert.Equal(row.ExceptionType, fingerprint.ExceptionType);
            Assert.Equal(row.SampleMessage, fingerprint.MessageTemplate);
            Assert.Equal(row.Count, fingerprint.TotalCount);
            Assert.Equal(row.LastSeen, fingerprint.FirstSeenUtc);
            Assert.Equal(row.LastSeen, fingerprint.LastSeenUtc);
            Assert.Equal(GithubIssueStatus.None, fingerprint.GithubStatus);
            _repositoryMock.Verify(x => x.Create(It.IsAny<Fingerprint>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessExceptionGroupAsync_WhenHashExists_IncrementsCountWithoutStagingAnUpdate()
        {
            var row = BuildExceptionRow(count: 5);
            var existing = new Fingerprint
            {
                Id = "fp_deadbeef",
                Hash = "deadbeef",
                Level = FingerprintLevel.Error,
                MessageTemplate = "old template",
                TotalCount = 10,
                FirstSeenUtc = DateTimeOffset.Parse("2026-07-01T00:00:00Z"),
                LastSeenUtc = DateTimeOffset.Parse("2026-07-01T00:00:00Z"),
                GithubStatus = GithubIssueStatus.None,
                CreatedAtUtc = DateTimeOffset.Parse("2026-07-01T00:00:00Z"),
                UpdatedAtUtc = DateTimeOffset.Parse("2026-07-01T00:00:00Z")
            };
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            var (fingerprint, isNew, windowCount) = await _service.ProcessExceptionGroupAsync(row, row.LastSeen, CancellationToken.None);

            Assert.False(isNew);
            Assert.Equal(row.Count, windowCount);
            Assert.Equal(15, fingerprint.TotalCount);
            Assert.Equal(row.LastSeen, fingerprint.LastSeenUtc);
            Assert.Equal("old template", fingerprint.MessageTemplate);
            // Deliberately no Update call. GetByHashAsync only ever returns tracked entities, so change
            // tracking persists these mutations - and on a fingerprint still Added from this batch,
            // DbSet.Update would flip the entry to Modified and emit an UPDATE for a row that was never
            // inserted (verified against EF Core 8.0.11).
            _repositoryMock.Verify(x => x.Update(It.IsAny<Fingerprint>()), Times.Never);
            _repositoryMock.Verify(x => x.Create(It.IsAny<Fingerprint>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessExceptionGroupAsync_AlwaysAddsOccurrence()
        {
            var row = BuildExceptionRow();
            var windowFrom = DateTimeOffset.Parse("2026-07-21T09:30:00Z");
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            await _service.ProcessExceptionGroupAsync(row, windowFrom, CancellationToken.None);

            _occurrenceRepositoryMock.Verify(x => x.Create(
                It.Is<FingerprintOccurrence>(o =>
                    o.OccurredAt == windowFrom &&
                    o.OccurrenceCount == row.Count &&
                    o.RenderedMessage == row.SampleMessage),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessTraceGroupAsync_WhenNewMessage_StoresNormalizedTemplateAndRawRenderedMessage()
        {
            var row = BuildTraceRow();
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var (fingerprint, isNew, _) = await _service.ProcessTraceGroupAsync(row, row.LastSeen, CancellationToken.None);

            Assert.True(isNew);
            Assert.Equal(FingerprintLevel.Warning, fingerprint.Level);
            Assert.Null(fingerprint.ExceptionType);
            Assert.Equal("Connection refused to 10.0.0.4:{n}", fingerprint.MessageTemplate);
            _occurrenceRepositoryMock.Verify(x => x.Create(
                It.Is<FingerprintOccurrence>(o => o.RenderedMessage == row.RawMessage),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessTraceGroupAsync_ErrorSeverity_MapsToErrorLevel()
        {
            // A LogError with no exception object lands in AppTraces at severity 3, not AppExceptions.
            // Level has to come from the severity, not from which table the row arrived in.
            var row = BuildTraceRow(severity: ErrorSeverity);
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var (fingerprint, _, _) = await _service.ProcessTraceGroupAsync(row, row.LastSeen, CancellationToken.None);

            Assert.Equal(FingerprintLevel.Error, fingerprint.Level);
        }

        [Fact]
        public async Task ProcessTraceGroupAsync_CriticalSeverity_MapsToErrorLevel()
        {
            // Critical folds into Error for triage; the raw severity still reaches the hash.
            var row = BuildTraceRow(severity: CriticalSeverity);
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var (fingerprint, _, _) = await _service.ProcessTraceGroupAsync(row, row.LastSeen, CancellationToken.None);

            Assert.Equal(FingerprintLevel.Error, fingerprint.Level);
        }

        [Fact]
        public async Task ProcessExceptionGroupAsync_WarningSeverity_MapsToWarningLevel()
        {
            // LogWarning(ex, …) produces an AppExceptions row at severity 2 — the observed majority
            // case in live telemetry. Filing those as Errors would inflate the openErrors stat.
            var row = BuildExceptionRow(severity: WarningSeverity);
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var (fingerprint, _, _) = await _service.ProcessExceptionGroupAsync(row, row.LastSeen, CancellationToken.None);

            Assert.Equal(FingerprintLevel.Warning, fingerprint.Level);
        }

        [Fact]
        public async Task ProcessTraceGroupAsync_SameMessageDifferentSeverity_ResolvesToDifferentFingerprints()
        {
            var warningRow = BuildTraceRow(rawMessage: "Payment gateway unreachable", severity: WarningSeverity);
            var errorRow = BuildTraceRow(rawMessage: "Payment gateway unreachable", severity: ErrorSeverity);
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var (warning, _, _) = await _service.ProcessTraceGroupAsync(warningRow, warningRow.LastSeen, CancellationToken.None);
            var (error, _, _) = await _service.ProcessTraceGroupAsync(errorRow, errorRow.LastSeen, CancellationToken.None);

            Assert.NotEqual(warning.Id, error.Id);
            Assert.Equal(FingerprintLevel.Warning, warning.Level);
            Assert.Equal(FingerprintLevel.Error, error.Level);
        }

        [Fact]
        public async Task ProcessTraceGroupAsync_SamePortVaryingValue_ResolvesToSameHash()
        {
            // Only the 4-digit port varies here; the IP octets are each under MinDigitRunLength (3) so
            // they stay literal after normalization - keeping them identical isolates the {n} substitution.
            var firstRow = BuildTraceRow(rawMessage: "Connection refused to 10.0.0.4:5432");
            var secondRow = BuildTraceRow(rawMessage: "Connection refused to 10.0.0.4:5433");
            string? capturedHash = null;
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((hash, _) => capturedHash = hash)
                .ReturnsAsync((Fingerprint?)null);

            var (first, _, _) = await _service.ProcessTraceGroupAsync(firstRow, firstRow.LastSeen, CancellationToken.None);
            var firstHash = capturedHash;
            var (second, _, _) = await _service.ProcessTraceGroupAsync(secondRow, secondRow.LastSeen, CancellationToken.None);

            Assert.Equal(firstHash, capturedHash);
            Assert.Equal(first.Id, second.Id);
        }

        [Fact]
        public async Task ProcessExceptionGroupAsync_WhenExceptionTypeExceedsMaxLength_Truncates()
        {
            var longExceptionType = new string('x', 600);
            var row = BuildExceptionRow(exceptionType: longExceptionType);
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var (fingerprint, _, _) = await _service.ProcessExceptionGroupAsync(row, row.LastSeen, CancellationToken.None);

            Assert.Equal(500, fingerprint.ExceptionType!.Length);
        }

        [Fact]
        public async Task ProcessExceptionGroupAsync_CallsClassifierWithProblemIdAndWindowCount()
        {
            var row = BuildExceptionRow(count: 7);
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            await _service.ProcessExceptionGroupAsync(row, row.LastSeen, CancellationToken.None);

            _classifierMock.Verify(x => x.ClassifyAsync(
                It.IsAny<Fingerprint>(), true, row.Count, row.ProblemId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessTraceGroupAsync_CallsClassifierWithNullProblemId()
        {
            var row = BuildTraceRow(count: 4);
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            await _service.ProcessTraceGroupAsync(row, row.LastSeen, CancellationToken.None);

            _classifierMock.Verify(x => x.ClassifyAsync(
                It.IsAny<Fingerprint>(), true, row.Count, null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
