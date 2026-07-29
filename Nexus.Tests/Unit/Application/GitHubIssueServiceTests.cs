using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Application.Common;
using Nexus.Application.Constants;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Application.Services;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Octokit;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class GitHubIssueServiceTests
    {
        private readonly Mock<IGitHubClient> _gitHubClientMock = new();
        private readonly Mock<IIssuesClient> _issuesClientMock = new();
        private readonly Mock<IIssueCommentsClient> _commentsClientMock = new();
        private readonly Mock<IIssuesLabelsClient> _labelsClientMock = new();
        private readonly Mock<IFingerprintAiService> _aiServiceMock = new();
        private readonly Mock<IFingerprintRouter> _routerMock = new();
        private readonly Mock<IFingerprintRepository> _fingerprintRepositoryMock = new();
        private readonly Mock<IFingerprintOccurrenceRepository> _occurrenceRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ILogger<GitHubIssueService>> _loggerMock = new();
        private readonly GitHubIssueService _service;

        public GitHubIssueServiceTests()
        {
            _gitHubClientMock.SetupGet(x => x.Issue).Returns(_issuesClientMock.Object);
            _issuesClientMock.SetupGet(x => x.Comment).Returns(_commentsClientMock.Object);
            _issuesClientMock.SetupGet(x => x.Labels).Returns(_labelsClientMock.Object);
            _issuesClientMock
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewIssue>()))
                .ReturnsAsync(new Issue());
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _routerMock.Setup(x => x.Route(It.IsAny<Fingerprint>())).Returns("default-owner");
            _occurrenceRepositoryMock
                .Setup(x => x.GetRecentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FingerprintOccurrence>());
            _aiServiceMock
                .Setup(x => x.SummarizeAsync(It.IsAny<Fingerprint>(), It.IsAny<IList<FingerprintOccurrence>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<FingerprintIssueContent>.Success(new FingerprintIssueContent
                {
                    Title = "issue title",
                    Body = "issue body",
                    SuggestedFix = "try restarting the dependency"
                }));

            var settings = Options.Create(new GitHubSettings { Token = "token", Owner = "owner", Repo = "repo" });

            _service = new GitHubIssueService(
                _gitHubClientMock.Object,
                _aiServiceMock.Object,
                _routerMock.Object,
                _fingerprintRepositoryMock.Object,
                _occurrenceRepositoryMock.Object,
                _uowMock.Object,
                settings,
                _loggerMock.Object);
        }

        private static Fingerprint BuildFingerprint(
            GithubIssueStatus githubStatus = GithubIssueStatus.None,
            int? githubIssueNumber = null,
            bool autoFixEligible = false,
            DateTimeOffset? lastCommentedAt = null,
            FingerprintCategory? category = FingerprintCategory.DependencyFailure) => new()
        {
            Id = "fp_deadbeef",
            Hash = "deadbeef",
            Level = FingerprintLevel.Error,
            MessageTemplate = "template",
            Category = category,
            AutoFixEligible = autoFixEligible,
            GithubStatus = githubStatus,
            GithubIssueNumber = githubIssueNumber,
            GithubLastCommentedAtUtc = lastCommentedAt,
            TotalCount = 10,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        [Fact]
        public async Task ProcessFingerprintAsync_WhenNoIssueAndBelowThresholdAndNotNew_DoesNothing()
        {
            var fingerprint = BuildFingerprint();

            await _service.ProcessFingerprintAsync(fingerprint, windowOccurrenceCount: 2, isNewRegression: false, CancellationToken.None);

            _issuesClientMock.Verify(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewIssue>()), Times.Never);
            Assert.Equal(GithubIssueStatus.None, fingerprint.GithubStatus);
        }

        [Fact]
        public async Task ProcessFingerprintAsync_WhenNewRegression_CreatesIssueEvenBelowThreshold()
        {
            var fingerprint = BuildFingerprint();

            await _service.ProcessFingerprintAsync(fingerprint, windowOccurrenceCount: 1, isNewRegression: true, CancellationToken.None);

            _issuesClientMock.Verify(x => x.Create("owner", "repo", It.IsAny<NewIssue>()), Times.Once);
            Assert.Equal(GithubIssueStatus.Open, fingerprint.GithubStatus);
            Assert.NotNull(fingerprint.GithubIssueNumber);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task ProcessFingerprintAsync_WhenNoIssueAtThreshold_CreatesIssueWithLabelsAndAssignee()
        {
            var fingerprint = BuildFingerprint();
            NewIssue? captured = null;
            _issuesClientMock
                .Setup(x => x.Create("owner", "repo", It.IsAny<NewIssue>()))
                .Callback<string, string, NewIssue>((_, _, issue) => captured = issue)
                .ReturnsAsync(new Issue());

            await _service.ProcessFingerprintAsync(
                fingerprint, windowOccurrenceCount: FingerprintFilingPolicy.MinOccurrencesToFile, isNewRegression: false, CancellationToken.None);

            Assert.NotNull(captured);
            Assert.Equal("issue title", captured!.Title);
            Assert.Contains("severity/error", captured.Labels);
            Assert.Contains("category/DEPENDENCY_FAILURE", captured.Labels);
            Assert.Contains("default-owner", captured.Assignees);
        }

        [Fact]
        public async Task ProcessFingerprintAsync_WhenAutoFixEligible_AppendsSuggestedFixToBody()
        {
            var fingerprint = BuildFingerprint(autoFixEligible: true);
            NewIssue? captured = null;
            _issuesClientMock
                .Setup(x => x.Create("owner", "repo", It.IsAny<NewIssue>()))
                .Callback<string, string, NewIssue>((_, _, issue) => captured = issue)
                .ReturnsAsync(new Issue());

            await _service.ProcessFingerprintAsync(fingerprint, windowOccurrenceCount: 3, isNewRegression: false, CancellationToken.None);

            Assert.Contains("## Suggested Fix", captured!.Body);
            Assert.Contains("try restarting the dependency", captured.Body);
        }

        [Fact]
        public async Task ProcessFingerprintAsync_WhenNotAutoFixEligible_OmitsSuggestedFix()
        {
            var fingerprint = BuildFingerprint(autoFixEligible: false);
            NewIssue? captured = null;
            _issuesClientMock
                .Setup(x => x.Create("owner", "repo", It.IsAny<NewIssue>()))
                .Callback<string, string, NewIssue>((_, _, issue) => captured = issue)
                .ReturnsAsync(new Issue());

            await _service.ProcessFingerprintAsync(fingerprint, windowOccurrenceCount: 3, isNewRegression: false, CancellationToken.None);

            Assert.DoesNotContain("## Suggested Fix", captured!.Body);
        }

        [Fact]
        public async Task ProcessFingerprintAsync_WhenIssueOpenAndThrottleElapsed_AddsCountComment()
        {
            var fingerprint = BuildFingerprint(
                GithubIssueStatus.Open, githubIssueNumber: 42,
                lastCommentedAt: DateTimeOffset.UtcNow.AddHours(-2));

            await _service.ProcessFingerprintAsync(fingerprint, windowOccurrenceCount: 5, isNewRegression: false, CancellationToken.None);

            _commentsClientMock.Verify(x => x.Create("owner", "repo", 42, It.Is<string>(c => c.Contains("5 occurrence"))), Times.Once);
            Assert.True(fingerprint.GithubLastCommentedAtUtc > DateTimeOffset.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task ProcessFingerprintAsync_WhenIssueOpenWithinThrottleWindow_DoesNotComment()
        {
            var lastCommented = DateTimeOffset.UtcNow.AddMinutes(-30);
            var fingerprint = BuildFingerprint(
                GithubIssueStatus.Open, githubIssueNumber: 42, lastCommentedAt: lastCommented);

            await _service.ProcessFingerprintAsync(fingerprint, windowOccurrenceCount: 5, isNewRegression: false, CancellationToken.None);

            _commentsClientMock.Verify(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            Assert.Equal(lastCommented, fingerprint.GithubLastCommentedAtUtc);
        }

        [Fact]
        public async Task ProcessFingerprintAsync_WhenIssueClosed_ReopensAndComments()
        {
            var fingerprint = BuildFingerprint(GithubIssueStatus.Closed, githubIssueNumber: 42);

            await _service.ProcessFingerprintAsync(fingerprint, windowOccurrenceCount: 5, isNewRegression: false, CancellationToken.None);

            _issuesClientMock.Verify(x => x.Update("owner", "repo", 42, It.Is<IssueUpdate>(u => u.State == ItemState.Open)), Times.Once);
            _commentsClientMock.Verify(x => x.Create("owner", "repo", 42, It.Is<string>(c => c.Contains("Regressed"))), Times.Once);
            Assert.Equal(GithubIssueStatus.Open, fingerprint.GithubStatus);
        }

        [Fact]
        public async Task ProcessFingerprintAsync_WhenGitHubThrows_SwallowsAndLeavesStateUnchanged()
        {
            var fingerprint = BuildFingerprint();
            _issuesClientMock
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewIssue>()))
                .ThrowsAsync(new ApiException());

            await _service.ProcessFingerprintAsync(fingerprint, windowOccurrenceCount: 5, isNewRegression: true, CancellationToken.None);

            Assert.Equal(GithubIssueStatus.None, fingerprint.GithubStatus);
            Assert.Null(fingerprint.GithubIssueNumber);
        }

        [Fact]
        public async Task ForceFileIssueAsync_WhenNoIssue_CreatesIgnoringThreshold()
        {
            var fingerprint = BuildFingerprint();

            var result = await _service.ForceFileIssueAsync(fingerprint, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _issuesClientMock.Verify(x => x.Create("owner", "repo", It.IsAny<NewIssue>()), Times.Once);
            Assert.Equal(GithubIssueStatus.Open, fingerprint.GithubStatus);
        }

        [Fact]
        public async Task ForceFileIssueAsync_WhenIssueAlreadyOpen_ReturnsConflict()
        {
            var fingerprint = BuildFingerprint(GithubIssueStatus.Open, githubIssueNumber: 42);

            var result = await _service.ForceFileIssueAsync(fingerprint, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            _issuesClientMock.Verify(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewIssue>()), Times.Never);
        }

        [Fact]
        public async Task ForceFileIssueAsync_WhenIssueClosed_Reopens()
        {
            var fingerprint = BuildFingerprint(GithubIssueStatus.Closed, githubIssueNumber: 42);

            var result = await _service.ForceFileIssueAsync(fingerprint, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _issuesClientMock.Verify(x => x.Update("owner", "repo", 42, It.Is<IssueUpdate>(u => u.State == ItemState.Open)), Times.Once);
            Assert.Equal(GithubIssueStatus.Open, fingerprint.GithubStatus);
        }

        [Fact]
        public async Task AddAutoFixCandidateLabelAsync_WhenEligibleWithIssue_AddsLabel()
        {
            var fingerprint = BuildFingerprint(GithubIssueStatus.Open, githubIssueNumber: 42, autoFixEligible: true);

            var result = await _service.AddAutoFixCandidateLabelAsync(fingerprint, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _labelsClientMock.Verify(x => x.AddToIssue("owner", "repo", 42,
                It.Is<string[]>(l => l.Contains(FingerprintConstants.AutoFixCandidateLabel))), Times.Once);
        }

        [Fact]
        public async Task AddAutoFixCandidateLabelAsync_WhenNotEligible_ReturnsConflict()
        {
            var fingerprint = BuildFingerprint(GithubIssueStatus.Open, githubIssueNumber: 42, autoFixEligible: false);

            var result = await _service.AddAutoFixCandidateLabelAsync(fingerprint, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            _labelsClientMock.Verify(x => x.AddToIssue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task AddAutoFixCandidateLabelAsync_WhenNoIssueNumber_ReturnsConflict()
        {
            var fingerprint = BuildFingerprint(autoFixEligible: true);

            var result = await _service.AddAutoFixCandidateLabelAsync(fingerprint, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            _labelsClientMock.Verify(x => x.AddToIssue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task CloseIssueAsync_WhenIssueOpen_ClosesIssueAndSetsStatus()
        {
            var fingerprint = BuildFingerprint(GithubIssueStatus.Open, githubIssueNumber: 42);

            var result = await _service.CloseIssueAsync(fingerprint, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(GithubIssueStatus.Closed, fingerprint.GithubStatus);
            _issuesClientMock.Verify(x => x.Update("owner", "repo", 42,
                It.Is<IssueUpdate>(u => u.State == ItemState.Closed)), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task CloseIssueAsync_WhenNoIssue_ReturnsConflict()
        {
            var fingerprint = BuildFingerprint();

            var result = await _service.CloseIssueAsync(fingerprint, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("NoGithubIssue", result.Errors.Single().Code);
            _issuesClientMock.Verify(x => x.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IssueUpdate>()), Times.Never);
        }

        [Fact]
        public async Task CloseIssueAsync_WhenAlreadyClosed_ReturnsConflict()
        {
            var fingerprint = BuildFingerprint(GithubIssueStatus.Closed, githubIssueNumber: 42);

            var result = await _service.CloseIssueAsync(fingerprint, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("AlreadyResolved", result.Errors.Single().Code);
        }

        [Fact]
        public async Task CloseIssueAsync_WhenPrInProgress_ReturnsConflict()
        {
            var fingerprint = BuildFingerprint(GithubIssueStatus.Pr, githubIssueNumber: 42);

            var result = await _service.CloseIssueAsync(fingerprint, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("PrInProgress", result.Errors.Single().Code);
            _issuesClientMock.Verify(x => x.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IssueUpdate>()), Times.Never);
        }

        [Fact]
        public async Task ProcessFingerprintAsync_WhenIssueCreated_SetsGithubIssueFiledAt()
        {
            var fingerprint = BuildFingerprint();

            await _service.ProcessFingerprintAsync(fingerprint, windowOccurrenceCount: 5, isNewRegression: false, CancellationToken.None);

            Assert.NotNull(fingerprint.GithubIssueFiledAtUtc);
        }
    }
}
