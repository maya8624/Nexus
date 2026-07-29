using Moq;
using Nexus.Application.Common;
using Nexus.Application.Constants;
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
    public class FingerprintQueryServiceTests
    {
        private readonly Mock<IFingerprintRepository> _fingerprintRepositoryMock = new();
        private readonly Mock<IFingerprintOccurrenceRepository> _occurrenceRepositoryMock = new();
        private readonly Mock<IGitHubIssueService> _gitHubIssueServiceMock = new();
        private readonly FingerprintQueryService _service;

        public FingerprintQueryServiceTests()
        {
            _fingerprintRepositoryMock
                .Setup(x => x.GetSparklineBucketsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FingerprintSparklineBucketReadModel>());
            _occurrenceRepositoryMock
                .Setup(x => x.GetRecentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FingerprintOccurrence>());

            _service = new FingerprintQueryService(
                _fingerprintRepositoryMock.Object,
                _occurrenceRepositoryMock.Object,
                _gitHubIssueServiceMock.Object);
        }

        private static Fingerprint BuildFingerprint(
            string id = "fp_deadbeef",
            GithubIssueStatus githubStatus = GithubIssueStatus.None) => new()
        {
            Id = id,
            Hash = "deadbeef",
            Level = FingerprintLevel.Error,
            Category = FingerprintCategory.DependencyFailure,
            ClassifiedBy = ClassificationSource.Rule,
            ExceptionType = "System.Net.Http.HttpRequestException",
            MessageTemplate = "Connection refused to {n}.{n}.{n}.{n}:{n}",
            ServiceName = "nexus-api-dev",
            Operation = "POST /api/internal/invoices/extract",
            TotalCount = 47,
            GithubStatus = githubStatus,
            FirstSeenUtc = DateTimeOffset.UtcNow.AddDays(-2),
            LastSeenUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        [Fact]
        public async Task GetListAsync_WhenFingerprintsExist_MapsFieldsAndSparkline()
        {
            var fingerprint = BuildFingerprint();
            _fingerprintRepositoryMock
                .Setup(x => x.GetListAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Fingerprint> { fingerprint });
            _fingerprintRepositoryMock
                .Setup(x => x.GetSparklineBucketsAsync(fingerprint.Id, FingerprintConstants.SparklineHistoryHours, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FingerprintSparklineBucketReadModel>
                {
                    new() { BucketStart = DateTimeOffset.UtcNow.AddHours(-1), Count = 12 }
                });

            var result = await _service.GetListAsync(null, null, CancellationToken.None);

            Assert.True(result.IsSuccess);
            var item = Assert.Single(result.Value!);
            Assert.Equal(fingerprint.Id, item.Id);
            Assert.Equal(fingerprint.Level, item.Level);
            Assert.Equal(fingerprint.Category, item.Category);
            Assert.Equal(fingerprint.TotalCount, item.TotalCount);
            Assert.Equal(12, Assert.Single(item.Sparkline).Count);
        }

        [Fact]
        public async Task GetListAsync_WhenFiltersProvided_PassesThemToRepository()
        {
            _fingerprintRepositoryMock
                .Setup(x => x.GetListAsync(GithubIssueStatus.Open, FingerprintLevel.Warning, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Fingerprint>());

            var result = await _service.GetListAsync(GithubIssueStatus.Open, FingerprintLevel.Warning, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
            _fingerprintRepositoryMock.Verify(
                x => x.GetListAsync(GithubIssueStatus.Open, FingerprintLevel.Warning, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
        {
            _fingerprintRepositoryMock
                .Setup(x => x.GetByIdAsync("fp_missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var result = await _service.GetByIdAsync("fp_missing", CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_ReturnsDetailWithOccurrences()
        {
            var fingerprint = BuildFingerprint();
            _fingerprintRepositoryMock
                .Setup(x => x.GetByIdAsync(fingerprint.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fingerprint);
            _occurrenceRepositoryMock
                .Setup(x => x.GetRecentAsync(fingerprint.Id, FingerprintConstants.MaxDetailOccurrences, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FingerprintOccurrence>
                {
                    new() { FingerprintId = fingerprint.Id, OccurredAt = DateTimeOffset.UtcNow, OccurrenceCount = 3, RenderedMessage = "Connection refused to 10.0.0.4:5432" }
                });

            var result = await _service.GetByIdAsync(fingerprint.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(fingerprint.MessageTemplate, result.Value!.MessageTemplate);
            Assert.Equal(fingerprint.ExceptionType, result.Value.ExceptionType);
            var occurrence = Assert.Single(result.Value.RecentOccurrences);
            Assert.Equal(3, occurrence.OccurrenceCount);
        }

        [Fact]
        public async Task FileIssueAsync_WhenNotFound_ReturnsNotFoundWithoutCallingGitHub()
        {
            _fingerprintRepositoryMock
                .Setup(x => x.GetByIdAsync("fp_missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Fingerprint?)null);

            var result = await _service.FileIssueAsync("fp_missing", CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            _gitHubIssueServiceMock.Verify(
                x => x.ForceFileIssueAsync(It.IsAny<Fingerprint>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FileIssueAsync_WhenGitHubServiceConflicts_PropagatesConflict()
        {
            var fingerprint = BuildFingerprint(githubStatus: GithubIssueStatus.Open);
            _fingerprintRepositoryMock
                .Setup(x => x.GetByIdAsync(fingerprint.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fingerprint);
            _gitHubIssueServiceMock
                .Setup(x => x.ForceFileIssueAsync(fingerprint, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Fingerprint>.Conflict("IssueAlreadyOpen", "A GitHub issue is already open for this fingerprint."));

            var result = await _service.FileIssueAsync(fingerprint.Id, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("IssueAlreadyOpen", result.Errors.Single().Code);
        }

        [Fact]
        public async Task FileIssueAsync_WhenSuccess_ReturnsDetail()
        {
            var fingerprint = BuildFingerprint();
            _fingerprintRepositoryMock
                .Setup(x => x.GetByIdAsync(fingerprint.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fingerprint);
            _gitHubIssueServiceMock
                .Setup(x => x.ForceFileIssueAsync(fingerprint, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Fingerprint>.Success(fingerprint));

            var result = await _service.FileIssueAsync(fingerprint.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(fingerprint.Id, result.Value!.Id);
        }

        [Fact]
        public async Task SendToAgentAsync_WhenGitHubServiceConflicts_PropagatesConflict()
        {
            var fingerprint = BuildFingerprint();
            _fingerprintRepositoryMock
                .Setup(x => x.GetByIdAsync(fingerprint.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fingerprint);
            _gitHubIssueServiceMock
                .Setup(x => x.AddAutoFixCandidateLabelAsync(fingerprint, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Fingerprint>.Conflict("NotAutoFixEligible", "This fingerprint is not eligible for auto-fix."));

            var result = await _service.SendToAgentAsync(fingerprint.Id, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("NotAutoFixEligible", result.Errors.Single().Code);
        }

        [Fact]
        public async Task ResolveAsync_WhenSuccess_DelegatesToCloseIssue()
        {
            var fingerprint = BuildFingerprint(githubStatus: GithubIssueStatus.Open);
            _fingerprintRepositoryMock
                .Setup(x => x.GetByIdAsync(fingerprint.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fingerprint);
            _gitHubIssueServiceMock
                .Setup(x => x.CloseIssueAsync(fingerprint, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Fingerprint>.Success(fingerprint));

            var result = await _service.ResolveAsync(fingerprint.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _gitHubIssueServiceMock.Verify(x => x.CloseIssueAsync(fingerprint, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetStatsAsync_MapsRepositoryCounts()
        {
            _fingerprintRepositoryMock
                .Setup(x => x.GetStatsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FingerprintStatsReadModel
                {
                    OpenErrors = 5,
                    OpenWarnings = 2,
                    IssuesFiledToday = 3,
                    PrsAwaitingReview = 0
                });

            var result = await _service.GetStatsAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.Value!.OpenErrors);
            Assert.Equal(2, result.Value.OpenWarnings);
            Assert.Equal(3, result.Value.IssuesAssignedToday);
            Assert.Equal(0, result.Value.AgentPrsAwaitingReview);
        }

        [Fact]
        public async Task GetStatsAsync_PassesUtcMidnightToRepository()
        {
            DateTimeOffset captured = default;
            _fingerprintRepositoryMock
                .Setup(x => x.GetStatsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Callback<DateTimeOffset, CancellationToken>((since, _) => captured = since)
                .ReturnsAsync(new FingerprintStatsReadModel());

            await _service.GetStatsAsync(CancellationToken.None);

            Assert.Equal(TimeSpan.Zero, captured.Offset);
            Assert.Equal(TimeSpan.Zero, captured.TimeOfDay);
            Assert.Equal(DateTimeOffset.UtcNow.Date, captured.Date);
        }
    }
}
