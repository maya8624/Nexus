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
        private readonly Mock<IFingerprintRepository> _fingerprintRepositoryMock = new();
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
            _fingerprintRepositoryMock
                .Setup(x => x.GetUnfiledSinceAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Fingerprint>());

            var settings = Options.Create(new FingerprintIngestSettings
            {
                WorkspaceId = "workspace-id",
                InitialLookbackMinutes = 60,
                IngestionSafetyLagMinutes = 5,
                MissedIssueLookbackHours = 24,
                MaxMissedIssueRetriesPerRun = 25
            });

            _job = new FingerprintIngestJob(
                _appInsightsMock.Object,
                _fingerprinterMock.Object,
                _gitHubIssueServiceMock.Object,
                _fingerprintRepositoryMock.Object,
                _cursorRepositoryMock.Object,
                _uowMock.Object,
                settings,
                _loggerMock.Object);
        }

        // The job groups staged rows by Fingerprint.Id, so tests covering separate fingerprints must pass
        // distinct ids or they collapse into one pending action.
        private static Fingerprint BuildFingerprint(string id = "fp_deadbeef") => new()
        {
            Id = id,
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
                .ReturnsAsync((BuildFingerprint("fp_aaaaaaaa"), true, exceptionRow.Count));
            _fingerprinterMock
                .Setup(x => x.ProcessTraceGroupAsync(traceRow, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BuildFingerprint("fp_bbbbbbbb"), false, traceRow.Count));

            await _job.ExecuteAsync();

            _fingerprinterMock.Verify(x => x.ProcessExceptionGroupAsync(exceptionRow, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            _fingerprinterMock.Verify(x => x.ProcessTraceGroupAsync(traceRow, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            _cursorRepositoryMock.Verify(x => x.Create(
                It.Is<IngestCursor>(c => c.Source == FingerprintConstants.IngestCursorSource),
                It.IsAny<CancellationToken>()), Times.Once);
            // A single commit covers every fingerprint, every occurrence, and the cursor.
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCommitTheWholeBatchBeforeInvokingTheGitHubActor()
        {
            // The actor creates a GitHub issue and then commits the resulting issue number through the
            // same unit of work. If any fingerprint is still unsaved at that point, its Added state turns
            // the actor's commit into an UPDATE of a row that never existed - taking the staged occurrence
            // down with a foreign key violation - and, worse, the issue is created before the fingerprint
            // is durable, so a failure in between leaves an orphaned issue that the next poll files all
            // over again. One commit covers the whole batch, so every row is durable before any external
            // side effect runs.
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
                .ReturnsAsync((BuildFingerprint("fp_aaaaaaaa"), true, exceptionRow.Count));
            _fingerprinterMock
                .Setup(x => x.ProcessTraceGroupAsync(traceRow, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BuildFingerprint("fp_bbbbbbbb"), false, traceRow.Count));

            var calls = new List<string>();
            _uowMock.Setup(x => x.SaveChanges())
                .Callback(() => calls.Add("save"))
                .ReturnsAsync(1);
            _gitHubIssueServiceMock
                .Setup(x => x.ProcessFingerprintAsync(It.IsAny<Fingerprint>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback(() => calls.Add("github"))
                .Returns(Task.CompletedTask);

            await _job.ExecuteAsync();

            Assert.Equal(["save", "github", "github"], calls);
        }

        [Fact]
        public async Task ExecuteAsync_WhenTraceRowsNormalizeToTheSameHash_ShouldMergeThemIntoOneRow()
        {
            // The trace query groups by raw message, so two filenames arrive as two rows - but
            // NormalizeMessage collapses both to "Blob {str} not found", one fingerprint. Merging before
            // staging is what keeps the fingerprinter from building a duplicate id, keeps the occurrence
            // table at one row per window, and gives the filing threshold the window's real total.
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);
            _appInsightsMock
                .Setup(x => x.QueryTraceGroupsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AppInsightsTraceGroupReadModel>
                {
                    new() { RawMessage = "Blob \"invoice-7781.pdf\" not found", Severity = 2, Count = 2, LastSeen = DateTimeOffset.UtcNow.AddMinutes(-10) },
                    new() { RawMessage = "Blob \"invoice-9932.pdf\" not found", Severity = 2, Count = 5, LastSeen = DateTimeOffset.UtcNow.AddMinutes(-5) }
                });
            _fingerprinterMock
                .Setup(x => x.ProcessTraceGroupAsync(It.IsAny<AppInsightsTraceGroupReadModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BuildFingerprint(), true, 7));

            await _job.ExecuteAsync();

            // One call, carrying the summed count - not two calls of 2 and 5.
            _fingerprinterMock.Verify(
                x => x.ProcessTraceGroupAsync(It.Is<AppInsightsTraceGroupReadModel>(r => r.Count == 7), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _fingerprinterMock.Verify(
                x => x.ProcessTraceGroupAsync(It.IsAny<AppInsightsTraceGroupReadModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenExceptionRowsShareProblemIdAndSeverity_ShouldMergeThemIntoOneRow()
        {
            // The exception query also groups by Operation and ServiceName, but the hash uses only
            // severity|problemId - so the same exception thrown from two operations is one fingerprint.
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);
            _appInsightsMock
                .Setup(x => x.QueryExceptionGroupsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AppInsightsExceptionGroupReadModel>
                {
                    new() { ProblemId = "problem-1", ExceptionType = "System.Exception", SampleMessage = "boom", Severity = 3, Operation = "POST /a", Count = 3, LastSeen = DateTimeOffset.UtcNow.AddMinutes(-10) },
                    new() { ProblemId = "problem-1", ExceptionType = "System.Exception", SampleMessage = "boom", Severity = 3, Operation = "POST /b", Count = 4, LastSeen = DateTimeOffset.UtcNow.AddMinutes(-5) }
                });
            _fingerprinterMock
                .Setup(x => x.ProcessExceptionGroupAsync(It.IsAny<AppInsightsExceptionGroupReadModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BuildFingerprint(), true, 7));

            await _job.ExecuteAsync();

            _fingerprinterMock.Verify(
                x => x.ProcessExceptionGroupAsync(It.Is<AppInsightsExceptionGroupReadModel>(r => r.Count == 7), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _fingerprinterMock.Verify(
                x => x.ProcessExceptionGroupAsync(It.IsAny<AppInsightsExceptionGroupReadModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenTraceRowsHaveDifferentSeverities_ShouldKeepThemSeparate()
        {
            // Severity is part of the hash: the same message logged as Warning and as Error is two
            // distinct fingerprints, so merging must not collapse them.
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);
            _appInsightsMock
                .Setup(x => x.QueryTraceGroupsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AppInsightsTraceGroupReadModel>
                {
                    new() { RawMessage = "Retry limit reached", Severity = 2, Count = 2, LastSeen = DateTimeOffset.UtcNow },
                    new() { RawMessage = "Retry limit reached", Severity = 3, Count = 5, LastSeen = DateTimeOffset.UtcNow }
                });
            _fingerprinterMock
                .Setup(x => x.ProcessTraceGroupAsync(It.IsAny<AppInsightsTraceGroupReadModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BuildFingerprint(), true, 1));

            await _job.ExecuteAsync();

            _fingerprinterMock.Verify(
                x => x.ProcessTraceGroupAsync(It.IsAny<AppInsightsTraceGroupReadModel>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
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

        [Fact]
        public async Task ExecuteAsync_WhenFingerprintsAreStillUnfiled_ShouldRetryThemBypassingTheThreshold()
        {
            // These already earned a "file it" from FingerprintFilingPolicy when they were created -
            // isNew short-circuits the occurrence threshold - and only failed to reach GitHub. The cursor
            // has advanced past their window, so this retry is their only remaining chance. A TotalCount
            // of 1 would fail ShouldFileIssue, which is exactly why isNewRegression must be passed true.
            var unfiled = BuildFingerprint("fp_11111111");
            unfiled.TotalCount = 1;
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);
            _fingerprintRepositoryMock
                .Setup(x => x.GetUnfiledSinceAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Fingerprint> { unfiled });

            await _job.ExecuteAsync();

            _gitHubIssueServiceMock.Verify(
                x => x.ProcessFingerprintAsync(unfiled, 1, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenAnUnfiledFingerprintIsAlsoInTheCurrentBatch_ShouldProcessItOnce()
        {
            // A row staged this run is still None until the actor files it, so the retry query returns it
            // too. Processing it twice would file the issue and then immediately comment on it, and the
            // swept copy would be a detached duplicate of a row this context already tracks.
            var shared = BuildFingerprint("fp_22222222");
            var exceptionRow = new AppInsightsExceptionGroupReadModel
            {
                ProblemId = "problem-1",
                ExceptionType = "System.Exception",
                SampleMessage = "boom",
                Count = 4,
                LastSeen = DateTimeOffset.UtcNow.AddMinutes(-10)
            };
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);
            _appInsightsMock
                .Setup(x => x.QueryExceptionGroupsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AppInsightsExceptionGroupReadModel> { exceptionRow });
            _fingerprinterMock
                .Setup(x => x.ProcessExceptionGroupAsync(exceptionRow, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((shared, true, exceptionRow.Count));
            _fingerprintRepositoryMock
                .Setup(x => x.GetUnfiledSinceAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Fingerprint> { shared });

            await _job.ExecuteAsync();

            _gitHubIssueServiceMock.Verify(
                x => x.ProcessFingerprintAsync(It.IsAny<Fingerprint>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenWindowHasNoEvents_ShouldStillRetryMissedFilings()
        {
            // A quiet window is precisely when a backlog should drain; bailing out early would strand it.
            var unfiled = BuildFingerprint("fp_33333333");
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);
            _fingerprintRepositoryMock
                .Setup(x => x.GetUnfiledSinceAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Fingerprint> { unfiled });

            await _job.ExecuteAsync();

            _gitHubIssueServiceMock.Verify(
                x => x.ProcessFingerprintAsync(unfiled, It.IsAny<int>(), true, It.IsAny<CancellationToken>()),
                Times.Once);
            // Nothing was staged, so the batch commit is still skipped.
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenMissedIssueLookbackIsNotPositive_ShouldNotQueryUnfiledFingerprints()
        {
            var job = BuildJob(new FingerprintIngestSettings
            {
                WorkspaceId = "workspace-id",
                InitialLookbackMinutes = 60,
                IngestionSafetyLagMinutes = 5,
                MissedIssueLookbackHours = 0,
                MaxMissedIssueRetriesPerRun = 25
            });
            _cursorRepositoryMock
                .Setup(x => x.GetAsync(FingerprintConstants.IngestCursorSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestCursor?)null);

            await job.ExecuteAsync();

            _fingerprintRepositoryMock.Verify(
                x => x.GetUnfiledSinceAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private FingerprintIngestJob BuildJob(FingerprintIngestSettings settings) => new(
            _appInsightsMock.Object,
            _fingerprinterMock.Object,
            _gitHubIssueServiceMock.Object,
            _fingerprintRepositoryMock.Object,
            _cursorRepositoryMock.Object,
            _uowMock.Object,
            Options.Create(settings),
            _loggerMock.Object);
    }
}
