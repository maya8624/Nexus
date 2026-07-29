using Nexus.Application.Common;
using Nexus.Application.Constants;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class FingerprintQueryService : IFingerprintService
    {
        private readonly IFingerprintRepository _fingerprintRepository;
        private readonly IFingerprintOccurrenceRepository _occurrenceRepository;
        private readonly IGitHubIssueService _gitHubIssueService;

        public FingerprintQueryService(
            IFingerprintRepository fingerprintRepository,
            IFingerprintOccurrenceRepository occurrenceRepository,
            IGitHubIssueService gitHubIssueService)
        {
            _fingerprintRepository = fingerprintRepository;
            _occurrenceRepository = occurrenceRepository;
            _gitHubIssueService = gitHubIssueService;
        }

        public async Task<Result<IList<FingerprintListItemResponse>>> GetListAsync(GithubIssueStatus? status, FingerprintLevel? level, CancellationToken ct)
        {
            var fingerprints = await _fingerprintRepository.GetListAsync(status, level, ct);

            var items = new List<FingerprintListItemResponse>(fingerprints.Count);
            foreach (var fingerprint in fingerprints)
            {
                var sparkline = await GetSparklineAsync(fingerprint.Id, ct);
                items.Add(MapListItem(fingerprint, sparkline));
            }

            return Result<IList<FingerprintListItemResponse>>.Success(items);
        }

        public async Task<Result<FingerprintDetailResponse>> GetByIdAsync(string id, CancellationToken ct)
        {
            var fingerprint = await _fingerprintRepository.GetByIdAsync(id, ct);
            if (fingerprint is null)
                return Result<FingerprintDetailResponse>.NotFound("FingerprintNotFound", $"Fingerprint '{id}' was not found.");

            return Result<FingerprintDetailResponse>.Success(await MapDetailAsync(fingerprint, ct));
        }

        public Task<Result<FingerprintDetailResponse>> FileIssueAsync(string id, CancellationToken ct)
        {
            return ExecuteGitHubActionAsync(id, _gitHubIssueService.ForceFileIssueAsync, ct);
        }

        public Task<Result<FingerprintDetailResponse>> SendToAgentAsync(string id, CancellationToken ct)
        {
            return ExecuteGitHubActionAsync(id, _gitHubIssueService.AddAutoFixCandidateLabelAsync, ct);
        }

        public Task<Result<FingerprintDetailResponse>> ResolveAsync(string id, CancellationToken ct)
        {
            return ExecuteGitHubActionAsync(id, _gitHubIssueService.CloseIssueAsync, ct);
        }

        public async Task<Result<FingerprintStatsResponse>> GetStatsAsync(CancellationToken ct)
        {
            var todayStartUtc = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
            var stats = await _fingerprintRepository.GetStatsAsync(todayStartUtc, ct);

            return Result<FingerprintStatsResponse>.Success(new FingerprintStatsResponse
            {
                OpenErrors = stats.OpenErrors,
                OpenWarnings = stats.OpenWarnings,
                IssuesAssignedToday = stats.IssuesFiledToday,
                AgentPrsAwaitingReview = stats.PrsAwaitingReview
            });
        }

        private async Task<Result<FingerprintDetailResponse>> ExecuteGitHubActionAsync(
            string id,
            Func<Fingerprint, CancellationToken, Task<Result<Fingerprint>>> action,
            CancellationToken ct)
        {
            var fingerprint = await _fingerprintRepository.GetByIdAsync(id, ct);
            if (fingerprint is null)
                return Result<FingerprintDetailResponse>.NotFound("FingerprintNotFound", $"Fingerprint '{id}' was not found.");

            var result = await action(fingerprint, ct);
            if (!result.IsSuccess)
                return PropagateFailure(result);

            return Result<FingerprintDetailResponse>.Success(await MapDetailAsync(fingerprint, ct));
        }

        private static Result<FingerprintDetailResponse> PropagateFailure(Result<Fingerprint> result)
        {
            var error = result.Errors.First();
            return result.Status switch
            {
                ResultStatus.NotFound => Result<FingerprintDetailResponse>.NotFound(error.Code, error.Message),
                _ => Result<FingerprintDetailResponse>.Conflict(error.Code, error.Message)
            };
        }

        private async Task<IList<FingerprintSparklineBucketResponse>> GetSparklineAsync(string fingerprintId, CancellationToken ct)
        {
            var buckets = await _fingerprintRepository.GetSparklineBucketsAsync(
                fingerprintId, FingerprintConstants.SparklineHistoryHours, ct);

            return buckets
                .Select(x => new FingerprintSparklineBucketResponse { BucketStart = x.BucketStart, Count = x.Count })
                .ToList();
        }

        private async Task<FingerprintDetailResponse> MapDetailAsync(Fingerprint fingerprint, CancellationToken ct)
        {
            var sparkline = await GetSparklineAsync(fingerprint.Id, ct);
            var occurrences = await _occurrenceRepository.GetRecentAsync(
                fingerprint.Id, FingerprintConstants.MaxDetailOccurrences, ct);

            return new FingerprintDetailResponse
            {
                Id = fingerprint.Id,
                Level = fingerprint.Level,
                Category = fingerprint.Category,
                ServiceName = fingerprint.ServiceName,
                Operation = fingerprint.Operation,
                TotalCount = fingerprint.TotalCount,
                FirstSeenUtc = fingerprint.FirstSeenUtc,
                LastSeenUtc = fingerprint.LastSeenUtc,
                GithubStatus = fingerprint.GithubStatus,
                GithubIssueNumber = fingerprint.GithubIssueNumber,
                AutoFixEligible = fingerprint.AutoFixEligible,
                Sparkline = sparkline,
                ExceptionType = fingerprint.ExceptionType,
                MessageTemplate = fingerprint.MessageTemplate,
                ClassifiedBy = fingerprint.ClassifiedBy,
                GithubIssueFiledAtUtc = fingerprint.GithubIssueFiledAtUtc,
                GithubLastCommentedAtUtc = fingerprint.GithubLastCommentedAtUtc,
                RecentOccurrences = occurrences
                    .Select(x => new FingerprintOccurrenceResponse
                    {
                        OccurredAt = x.OccurredAt,
                        OccurrenceCount = x.OccurrenceCount,
                        RenderedMessage = x.RenderedMessage
                    })
                    .ToList()
            };
        }

        private static FingerprintListItemResponse MapListItem(Fingerprint fingerprint, IList<FingerprintSparklineBucketResponse> sparkline)
        {
            return new FingerprintListItemResponse
            {
                Id = fingerprint.Id,
                Level = fingerprint.Level,
                Category = fingerprint.Category,
                ServiceName = fingerprint.ServiceName,
                Operation = fingerprint.Operation,
                TotalCount = fingerprint.TotalCount,
                FirstSeenUtc = fingerprint.FirstSeenUtc,
                LastSeenUtc = fingerprint.LastSeenUtc,
                GithubStatus = fingerprint.GithubStatus,
                GithubIssueNumber = fingerprint.GithubIssueNumber,
                AutoFixEligible = fingerprint.AutoFixEligible,
                Sparkline = sparkline
            };
        }
    }
}
