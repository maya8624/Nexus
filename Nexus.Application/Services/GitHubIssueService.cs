using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Constants;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Octokit;

namespace Nexus.Application.Services
{
    public class GitHubIssueService : IGitHubIssueService
    {
        private readonly IGitHubClient _gitHubClient;
        private readonly IFingerprintAiService _fingerprintAiService;
        private readonly IFingerprintRouter _router;
        private readonly IFingerprintRepository _fingerprintRepository;
        private readonly IFingerprintOccurrenceRepository _occurrenceRepository;
        private readonly IUnitOfWork _uow;
        private readonly GitHubSettings _settings;
        private readonly ILogger<GitHubIssueService> _logger;

        public GitHubIssueService(
            IGitHubClient gitHubClient,
            IFingerprintAiService fingerprintAiService,
            IFingerprintRouter router,
            IFingerprintRepository fingerprintRepository,
            IFingerprintOccurrenceRepository occurrenceRepository,
            IUnitOfWork uow,
            IOptions<GitHubSettings> settings,
            ILogger<GitHubIssueService> logger)
        {
            _gitHubClient = gitHubClient;
            _fingerprintAiService = fingerprintAiService;
            _router = router;
            _fingerprintRepository = fingerprintRepository;
            _occurrenceRepository = occurrenceRepository;
            _uow = uow;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task ProcessFingerprintAsync(Fingerprint fingerprint, int windowOccurrenceCount, bool isNewRegression, CancellationToken ct)
        {
            try
            {
                switch (fingerprint.GithubStatus)
                {
                    case GithubIssueStatus.None when FingerprintFilingPolicy.ShouldFileIssue(windowOccurrenceCount, isNewRegression):
                        await CreateIssueAsync(fingerprint, ct);
                        break;
                    case GithubIssueStatus.Open:
                        await AddThrottledCountCommentAsync(fingerprint, windowOccurrenceCount, ct);
                        break;
                    case GithubIssueStatus.Closed:
                        await ReopenAsync(
                            fingerprint,
                            $"Regressed: fingerprint `{fingerprint.Id}` reappeared with {windowOccurrenceCount} occurrence(s) in the latest poll window after the issue was closed.",
                            ct);
                        break;
                    // None below threshold, Pr, Merged: no-op.
                }
            }
            catch (Exception ex)
            {
                // Swallowed deliberately: one flaky GitHub/AI call must not abort the whole poll
                // batch. The fingerprint keeps its current GitHub state and is retried next poll.
                _logger.LogWarning(ex, "GitHub issue processing failed for fingerprint {FingerprintId}; skipping this cycle.", fingerprint.Id);
            }
        }

        public async Task<Result<Fingerprint>> ForceFileIssueAsync(Fingerprint fingerprint, CancellationToken ct)
        {
            switch (fingerprint.GithubStatus)
            {
                case GithubIssueStatus.None:
                    await CreateIssueAsync(fingerprint, ct);
                    return Result<Fingerprint>.Success(fingerprint);
                case GithubIssueStatus.Closed:
                    await ReopenAsync(fingerprint, "Reopened via manual file-issue request while the existing issue was closed.", ct);
                    return Result<Fingerprint>.Success(fingerprint);
                default:
                    return Result<Fingerprint>.Conflict("IssueAlreadyOpen", "A GitHub issue is already open for this fingerprint.");
            }
        }

        public async Task<Result<Fingerprint>> AddAutoFixCandidateLabelAsync(Fingerprint fingerprint, CancellationToken ct)
        {
            if (!fingerprint.AutoFixEligible)
                return Result<Fingerprint>.Conflict("NotAutoFixEligible", "This fingerprint is not eligible for auto-fix.");

            if (fingerprint.GithubIssueNumber is null)
                return Result<Fingerprint>.Conflict("NoGithubIssue", "This fingerprint has no GitHub issue to label.");

            await _gitHubClient.Issue.Labels.AddToIssue(
                _settings.Owner, _settings.Repo, fingerprint.GithubIssueNumber.Value, [FingerprintConstants.AutoFixCandidateLabel]);

            return Result<Fingerprint>.Success(fingerprint);
        }

        private async Task CreateIssueAsync(Fingerprint fingerprint, CancellationToken ct)
        {
            var occurrences = await _occurrenceRepository.GetRecentAsync(
                fingerprint.Id, FingerprintConstants.MaxSummarizeOccurrences, ct);
            var contentResult = await _fingerprintAiService.SummarizeAsync(fingerprint, occurrences, ct);
            var content = contentResult.Value!;

            var body = content.Body;
            if (fingerprint.AutoFixEligible && !string.IsNullOrWhiteSpace(content.SuggestedFix))
                body = $"{body}\n\n## Suggested Fix\n{content.SuggestedFix}";

            var newIssue = new NewIssue(content.Title) { Body = body };
            newIssue.Labels.Add($"severity/{fingerprint.Level.ToString().ToLowerInvariant()}");
            if (fingerprint.Category is not null)
                newIssue.Labels.Add($"category/{FingerprintCategoryWireFormat.ToWire(fingerprint.Category.Value)}");
            newIssue.Assignees.Add(_router.Route(fingerprint));

            var issue = await _gitHubClient.Issue.Create(_settings.Owner, _settings.Repo, newIssue);

            fingerprint.GithubIssueNumber = issue.Number;
            fingerprint.GithubStatus = GithubIssueStatus.Open;
            fingerprint.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _fingerprintRepository.Update(fingerprint);
            // Committed immediately rather than with the poll batch: a crash before the batch commit
            // would lose the issue number and the next poll would file a duplicate issue.
            await _uow.SaveChanges();
        }

        private async Task AddThrottledCountCommentAsync(Fingerprint fingerprint, int windowOccurrenceCount, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            if (fingerprint.GithubLastCommentedAtUtc is not null &&
                now - fingerprint.GithubLastCommentedAtUtc.Value < TimeSpan.FromHours(FingerprintConstants.MinHoursBetweenComments))
                return;

            await _gitHubClient.Issue.Comment.Create(
                _settings.Owner, _settings.Repo, fingerprint.GithubIssueNumber!.Value,
                $"Still occurring: {windowOccurrenceCount} occurrence(s) in the latest poll window ({fingerprint.TotalCount} total).");

            fingerprint.GithubLastCommentedAtUtc = now;
            fingerprint.UpdatedAtUtc = now;
            _fingerprintRepository.Update(fingerprint);
            await _uow.SaveChanges();
        }

        private async Task ReopenAsync(Fingerprint fingerprint, string comment, CancellationToken ct)
        {
            var issueNumber = fingerprint.GithubIssueNumber!.Value;
            await _gitHubClient.Issue.Update(
                _settings.Owner, _settings.Repo, issueNumber, new IssueUpdate { State = ItemState.Open });
            await _gitHubClient.Issue.Comment.Create(_settings.Owner, _settings.Repo, issueNumber, comment);

            var now = DateTimeOffset.UtcNow;
            fingerprint.GithubStatus = GithubIssueStatus.Open;
            fingerprint.GithubLastCommentedAtUtc = now;
            fingerprint.UpdatedAtUtc = now;
            _fingerprintRepository.Update(fingerprint);
            await _uow.SaveChanges();
        }
    }
}
