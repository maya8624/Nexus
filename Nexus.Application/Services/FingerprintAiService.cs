using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Application.ReadModels;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Network;
using Nexus.Network.Interfaces;

namespace Nexus.Application.Services
{
    public class FingerprintAiService : IFingerprintAiService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly AiServiceSettings _settings;
        private readonly ILogger<FingerprintAiService> _logger;

        public FingerprintAiService(
            IHttpClientService httpClientService,
            IOptions<AiServiceSettings> settings,
            ILogger<FingerprintAiService> logger)
        {
            _httpClientService = httpClientService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<Result<FingerprintClassificationResult>> ClassifyAsync(Fingerprint fingerprint, CancellationToken ct)
        {
            var aiRequest = new AiFingerprintClassifyRequest
            {
                exception_type = fingerprint.ExceptionType,
                message_template = fingerprint.MessageTemplate,
                sample_trace = null,
                operation = fingerprint.Operation
            };

            var options = AiRequestOptionsFactory.Build(_settings, aiRequest, _settings.Classify);

            try
            {
                var httpRequest = HttpRequestFactory.CreateHttpRequestMessage(options);
                var raw = await _httpClientService.ExecuteRequest<AiFingerprintClassifyResponse>(httpRequest, ct);

                return Result<FingerprintClassificationResult>.Success(new FingerprintClassificationResult
                {
                    Category = FingerprintCategoryWireFormat.FromWire(raw.category),
                    Confidence = raw.confidence,
                    Reason = raw.reason,
                    Source = ClassificationSource.Llm
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI fingerprint classification failed for fingerprint {FingerprintId}", fingerprint.Id);
                throw new FingerprintAiServiceException("The AI service is currently unavailable. Please try again later.", ex);
            }
        }

        public async Task<Result<FingerprintIssueContent>> SummarizeAsync(
            Fingerprint fingerprint, IList<FingerprintOccurrence> recentOccurrences, CancellationToken ct)
        {
            var aiRequest = new AiFingerprintSummarizeRequest
            {
                fingerprint = new AiFingerprintSummarizeFingerprint
                {
                    id = fingerprint.Id,
                    level = fingerprint.Level.ToString().ToLowerInvariant(),
                    category = fingerprint.Category is null ? null : FingerprintCategoryWireFormat.ToWire(fingerprint.Category.Value),
                    exception_type = fingerprint.ExceptionType,
                    message_template = fingerprint.MessageTemplate,
                    operation = fingerprint.Operation,
                    service_name = fingerprint.ServiceName,
                    total_count = fingerprint.TotalCount,
                    first_seen = fingerprint.FirstSeenUtc,
                    last_seen = fingerprint.LastSeenUtc
                },
                occurrences = recentOccurrences.Select(x => new AiFingerprintSummarizeOccurrence
                {
                    occurred_at = x.OccurredAt,
                    occurrence_count = x.OccurrenceCount,
                    rendered_message = x.RenderedMessage
                }).ToList()
            };

            var options = AiRequestOptionsFactory.Build(_settings, aiRequest, _settings.Summarize);

            try
            {
                var httpRequest = HttpRequestFactory.CreateHttpRequestMessage(options);
                var raw = await _httpClientService.ExecuteRequest<AiFingerprintSummarizeResponse>(httpRequest, ct);

                return Result<FingerprintIssueContent>.Success(new FingerprintIssueContent
                {
                    Title = raw.title,
                    Body = raw.body,
                    SuggestedFix = raw.suggested_fix
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI fingerprint summarize failed for fingerprint {FingerprintId}", fingerprint.Id);
                throw new FingerprintAiServiceException("The AI service is currently unavailable. Please try again later.", ex);
            }
        }
    }
}
