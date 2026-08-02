using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Application.Constants;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Application.Services;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class FingerprintIngestJobTests
    {
        private readonly Mock<IAppInsightsQueryService> _appInsightsMock = new();
        private readonly Mock<IFingerprinterService> _fingerprinterMock = new();
        private readonly Mock<IGitHubIssueService> _gitHubIssueServiceMock = new();
        private readonly Mock<IIngestCursorRepository> _cursorRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ILogger<FingerprintIngestJob>> _loggerMock = new();
        private readonly FingerprintIngestJob _job;

        public FingerprintIngestJobTests()
        {
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _appInsightsMock
                .Setup(x => x.QueryExceptionGroupsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AppInsightsExceptionGroupReadModel>());
            _appInsightsMock
                .Setup(x => x.QueryTraceGroupsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AppInsightsTraceGroupReadModel>());

            var settings = Options.Create(new FingerprintIngestSettings
            {
                WorkspaceId = "workspace-id",
                InitialLookbackMinutes = 60,
                IngestionSafetyLagMinutes = 5
            });

            _job = new FingerprintIngestJob(
                _appInsightsMock.Object,
                _fingerprinterMock.Object,
                _gitHubIssueServiceMock.Object,
                _cursorRepositoryMock.Object,
                _uowMock.Object,
                settings,
                _loggerMock.Object);
        }

        private static Fingerprint BuildFingerprint() => new()
        {
            Id = "fp_deadbeef",
            Hash = "deadbeef",
            Level = FingerprintLevel.Error,
            MessageTemplate = "template",
            GithubStatus = GithubIssueStatus.None,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        [Fact]
        public async Task ExecuteAsync_WhenNoCursorExists_UsesInitialLookbackWindow()
        {
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);

            await _job.ExecuteAsync();

            _appInsightsMock.Verify(x => x.QueryExceptionGroupsAsync(
                It.Is<DateTimeOffset>(from => from <= DateTimeOffset.UtcNow.AddMinutes(-59)),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenCursorExists_UsesCursorAsWindowStart()
        {
            var lastPolledTo = DateTimeOffset.UtcNow.AddMinutes(-20);
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IngestCursor { Source = FingerprintConstants.IngestCursorSource, LastPolledTo = lastPolledTo, UpdatedAtUtc = lastPolledTo });

            await _job.ExecuteAsync();

            _appInsightsMock.Verify(x => x.QueryExceptionGroupsAsync(lastPolledTo, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoNewData_ShouldNotAdvanceCursor()
        {
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);

            await _job.ExecuteAsync();

            _cursorRepositoryMock.Verify(x => x.Create(It.IsAny<IngestCursor>(), It.IsAny<CancellationToken>()), Times.Never);
            _cursorRepositoryMock.Verify(x => x.Update(It.IsAny<IngestCursor>()), Times.Never);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenDataExists_ShouldProcessRowsAndAdvanceCursor()
        {
            var exceptionRow = new AppInsightsExceptionGroupReadModel
            {
                ProblemId = "problem-1",
                ExceptionType = "System.Exception",
                SampleMessage = "boom",
                Count = 3,
                LastSeen = DateTimeOffset.UtcNow.AddMinutes(-10)
            };
            var traceRow = new AppInsightsTraceGroupReadModel
            {
                RawMessage = "warn",
                Count = 2,
                LastSeen = DateTimeOffset.UtcNow.AddMinutes(-10)
            };
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);
            _appInsightsMock
                .Setup(x => x.QueryExceptionGroupsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AppInsightsExceptionGroupReadModel> { exceptionRow });
            _appInsightsMock
                .Setup(x => x.QueryTraceGroupsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AppInsightsTraceGroupReadModel> { traceRow });
            _fingerprinterMock
                .Setup(x => x.ProcessExceptionGroupAsync(exceptionRow, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BuildFingerprint(), true, exceptionRow.Count));
            _fingerprinterMock
                .Setup(x => x.ProcessTraceGroupAsync(traceRow, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BuildFingerprint(), false, traceRow.Count));

            await _job.ExecuteAsync();

            _fingerprinterMock.Verify(x => x.ProcessExceptionGroupAsync(exceptionRow, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            _fingerprinterMock.Verify(x => x.ProcessTraceGroupAsync(traceRow, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            _cursorRepositoryMock.Verify(x => x.Create(
                It.Is<IngestCursor>(c => c.Source == FingerprintConstants.IngestCursorSource),
                It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteAsync_WhenWindowStartIsAfterWindowEnd_ShouldNoOp()
        {
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IngestCursor
                {
                    Source = FingerprintConstants.IngestCursorSource,
                    LastPolledTo = DateTimeOffset.UtcNow.AddMinutes(1),
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                });

            await _job.ExecuteAsync();

            _appInsightsMock.Verify(x => x.QueryExceptionGroupsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }
    }
}
