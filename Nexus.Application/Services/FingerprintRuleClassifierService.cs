using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Constants;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class FingerprintRuleClassifierService : IFingerprintClassifier
    {
        // Timeouts are classified as DependencyFailure, not Performance - a timeout calling an
        // outbound dependency is a dependency problem, not internal slowness, given the data available.
        private static readonly string[] DependencyFailureExceptionTypes =
        [
            "HttpRequestException", 
            "SocketException", 
            "WebException", 
            "TaskCanceledException", 
            "TimeoutException"
        ];

        private static readonly string[] DependencyFailureMessageKeywords = ["connection refused"];

        private static readonly string[] ConfigAuthExceptionTypes =
        [
            "UnauthorizedAccessException", 
            "AuthenticationException", 
            "SecurityException"
        ];
        
        private static readonly string[] ConfigAuthMessageKeywords =
        [
            "unauthorized", 
            "forbidden", 
            "invalid api key", 
            "invalid token", 
            "authentication failed"
        ];

        private static readonly string[] DataQualityExceptionTypes =
        [
            "JsonException", 
            "FormatException", 
            "ArgumentException", 
            "ValidationException", 
            "SerializationException"
        ];
        
        private static readonly string[] DataQualityMessageKeywords =
        [
            "invalid format", 
            "deserializ", 
            "malformed", 
            "validation failed"
        ];

        private static readonly string[] PerformanceMessageKeywords =
        [
            "slow", 
            "exceeded threshold", 
            "latency", 
            "took longer than"
        ];

        private readonly IFingerprintRepository _fingerprintRepository;
        private readonly IFingerprintAiService _fingerprintAiService;
        private readonly FingerprintRoutingSettings _settings;
        private readonly ILogger<FingerprintRuleClassifierService> _logger;

        public FingerprintRuleClassifierService(
            IFingerprintRepository fingerprintRepository,
            IFingerprintAiService fingerprintAiService,
            IOptions<FingerprintRoutingSettings> settings,
            ILogger<FingerprintRuleClassifierService> logger)
        {
            _fingerprintRepository = fingerprintRepository;
            _fingerprintAiService = fingerprintAiService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task ClassifyAsync(Fingerprint fingerprint, bool isNewFingerprint, int windowOccurrenceCount, string? problemId, CancellationToken ct)
        {
            if (isNewFingerprint)
            {
                SetCategory(fingerprint, FingerprintCategory.NewRegression, ClassificationSource.Rule, problemId);
                return;
            }

            if (ContainsAny(fingerprint.ExceptionType, DependencyFailureExceptionTypes) || ContainsAny(fingerprint.MessageTemplate, DependencyFailureMessageKeywords))
            {
                SetCategory(fingerprint, FingerprintCategory.DependencyFailure, ClassificationSource.Rule, problemId);
                return;
            }

            if (ContainsAny(fingerprint.ExceptionType, ConfigAuthExceptionTypes) || ContainsAny(fingerprint.MessageTemplate, ConfigAuthMessageKeywords))
            {
                SetCategory(fingerprint, FingerprintCategory.ConfigAuth, ClassificationSource.Rule, problemId);
                return;
            }

            if (ContainsAny(fingerprint.ExceptionType, DataQualityExceptionTypes) || ContainsAny(fingerprint.MessageTemplate, DataQualityMessageKeywords))
            {
                SetCategory(fingerprint, FingerprintCategory.DataQuality, ClassificationSource.Rule, problemId);
                return;
            }

            if (ContainsAny(fingerprint.MessageTemplate, PerformanceMessageKeywords))
            {
                SetCategory(fingerprint, FingerprintCategory.Performance, ClassificationSource.Rule, problemId);
                return;
            }

            // GetHourlyBaselineAsync averages the fingerprint's whole occurrence history (no recent-window
            // bound) - an accepted MVP tradeoff; it gets more accurate as history accumulates, not less.
            var baseline = await _fingerprintRepository.GetHourlyBaselineAsync(fingerprint.Id, ct);
            if (baseline is not null && windowOccurrenceCount >= baseline.AverageHourlyCount * FingerprintConstants.MinSpikeMultiplier)
            {
                SetCategory(fingerprint, FingerprintCategory.RecurringKnown, ClassificationSource.Rule, problemId);
                return;
            }

            // Classify once, sticky: only call the LLM while still unclassified. A later rule match on a
            // subsequent poll can still override an earlier LLM classification.
            if (fingerprint.Category is not null)
                return;

            try
            {
                var result = await _fingerprintAiService.ClassifyAsync(fingerprint, ct);
                if (result.IsSuccess && result.Value is not null)
                    SetCategory(fingerprint, result.Value.Category, result.Value.Source, problemId);
            }
            catch (FingerprintAiServiceException ex)
            {
                // Swallowed deliberately: one flaky AI call must not abort the whole poll batch's SaveChanges.
                // The fingerprint is left unclassified and re-attempted on the next poll cycle.
                _logger.LogWarning(ex, "Fingerprint AI classification unavailable for fingerprint {FingerprintId}; leaving unclassified.", fingerprint.Id);
            }
        }

        private void SetCategory(Fingerprint fingerprint, FingerprintCategory category, ClassificationSource source, string? problemId)
        {
            fingerprint.Category = category;
            fingerprint.ClassifiedBy = source;
            fingerprint.AutoFixEligible = ComputeAutoFixEligible(category, problemId);
        }

        private bool ComputeAutoFixEligible(FingerprintCategory category, string? problemId)
        {
            if (problemId is null)
                return false;

            var wireCategory = FingerprintCategoryWireFormat.ToWire(category);
            if (!_settings.AutoFixAllowlistCategories.Contains(wireCategory))
                return false;

            return !_settings.AutoFixDenylistNamespaces.Any(x => problemId.StartsWith(x, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ContainsAny(string? text, IEnumerable<string> keywords)
            => text is not null && keywords.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
}
