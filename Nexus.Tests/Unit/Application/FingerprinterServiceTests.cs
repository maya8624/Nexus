using Moq;
using Nexus.Application.Common;
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
        private readonly FingerprinterService _service;

        public FingerprinterServiceTests()
        {
            _service = new FingerprinterService(_repositoryMock.Object, _occurrenceRepositoryMock.Object);
        }

        private static AppInsightsExceptionGroupReadModel BuildExceptionRow(
            string problemId = "Nexus.Application.Services.InvoiceExtractionJob!ExecuteAsync",
            string exceptionType = "System.Net.Http.HttpRequestException",
            int count = 14) => new()
        {
            ProblemId = problemId,
            ExceptionType = exceptionType,
            Operation = "POST /api/internal/invoices/extract",
            ServiceName = "nexus-api-dev",
            SampleMessage = "Response status code does not indicate success: 503 (Service Unavailable).",
            Count = count,
            LastSeen = DateTimeOffset.Parse("2026-07-21T09:52:11Z")
        };

        private static AppInsightsTraceWarningGroupReadModel BuildTraceRow(
            string rawMessage = "Connection refused to 10.0.0.4:5432",
            int count = 12) => new()
        {
            RawMessage = rawMessage,
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
        public async Task ProcessExceptionGroupAsync_WhenHashExists_IncrementsCountAndUpdates()
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
            _repositoryMock.Verify(x => x.Update(existing), Times.Once);
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
        public async Task ProcessTraceWarningGroupAsync_WhenNewMessage_StoresNormalizedTemplateAndRawRenderedMessage()
        {
            var row = BuildTraceRow();
            _repositoryMock
                .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var (fingerprint, isNew, _) = await _service.ProcessTraceWarningGroupAsync(row, row.LastSeen, CancellationToken.None);

            Assert.True(isNew);
            Assert.Equal(FingerprintLevel.Warning, fingerprint.Level);
            Assert.Null(fingerprint.ExceptionType);
            Assert.Equal("Connection refused to 10.0.0.4:{n}", fingerprint.MessageTemplate);
            _occurrenceRepositoryMock.Verify(x => x.Create(
                It.Is<FingerprintOccurrence>(o => o.RenderedMessage == row.RawMessage),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessTraceWarningGroupAsync_SamePortVaryingValue_ResolvesToSameHash()
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

            var (first, _, _) = await _service.ProcessTraceWarningGroupAsync(firstRow, firstRow.LastSeen, CancellationToken.None);
            var firstHash = capturedHash;
            var (second, _, _) = await _service.ProcessTraceWarningGroupAsync(secondRow, secondRow.LastSeen, CancellationToken.None);

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
    }
}
