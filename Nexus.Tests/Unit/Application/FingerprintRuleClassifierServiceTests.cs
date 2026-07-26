using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Application.Common;
using Nexus.Application.Constants;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
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
    public class FingerprintRuleClassifierServiceTests
    {
        private readonly Mock<IFingerprintRepository> _repositoryMock = new();
        private readonly Mock<IFingerprintAiService> _aiServiceMock = new();
        private readonly Mock<ILogger<FingerprintRuleClassifierService>> _loggerMock = new();

        private FingerprintRuleClassifierService BuildClassifier(List<string>? allowlist = null, List<string>? denylist = null, string defaultAssignee = "default-owner")
        {
            var settings = Options.Create(new FingerprintRoutingSettings
            {
                DefaultAssignee = defaultAssignee,
                AutoFixAllowlistCategories = allowlist ?? [],
                AutoFixDenylistNamespaces = denylist ?? []
            });

            return new FingerprintRuleClassifierService(_repositoryMock.Object, _aiServiceMock.Object, settings, _loggerMock.Object);
        }

        private static Fingerprint BuildFingerprint(
            string id = "fp_deadbeef",
            string? exceptionType = null,
            string messageTemplate = "template",
            FingerprintCategory? category = null) => new()
        {
            Id = id,
            Hash = "deadbeef",
            Level = FingerprintLevel.Error,
            ExceptionType = exceptionType,
            MessageTemplate = messageTemplate,
            Category = category,
            GithubStatus = GithubIssueStatus.None,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        [Fact]
        public async Task ClassifyAsync_WhenNewFingerprint_SetsNewRegressionAndSkipsRepositoryAndAi()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint();

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: true, windowOccurrenceCount: 1, problemId: "Nexus.Foo!Bar", CancellationToken.None);

            Assert.Equal(FingerprintCategory.NewRegression, fingerprint.Category);
            Assert.Equal(ClassificationSource.Rule, fingerprint.ClassifiedBy);
            _repositoryMock.Verify(x => x.GetHourlyBaselineAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _aiServiceMock.Verify(x => x.ClassifyAsync(It.IsAny<Fingerprint>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ClassifyAsync_WhenExceptionTypeIndicatesDependencyFailure_SetsDependencyFailure()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint(exceptionType: "System.Net.Http.HttpRequestException");

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: null, CancellationToken.None);

            Assert.Equal(FingerprintCategory.DependencyFailure, fingerprint.Category);
            Assert.Equal(ClassificationSource.Rule, fingerprint.ClassifiedBy);
        }

        [Fact]
        public async Task ClassifyAsync_WhenMessageIndicatesConnectionRefused_SetsDependencyFailure()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint(messageTemplate: "Connection refused to 10.0.0.4:{n}");

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: null, CancellationToken.None);

            Assert.Equal(FingerprintCategory.DependencyFailure, fingerprint.Category);
        }

        [Fact]
        public async Task ClassifyAsync_WhenMessageIndicatesConfigAuth_SetsConfigAuth()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint(messageTemplate: "Request was unauthorized for this resource");

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: null, CancellationToken.None);

            Assert.Equal(FingerprintCategory.ConfigAuth, fingerprint.Category);
        }

        [Fact]
        public async Task ClassifyAsync_WhenMessageIndicatesDataQuality_SetsDataQuality()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint(messageTemplate: "Malformed payload could not be processed");

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: null, CancellationToken.None);

            Assert.Equal(FingerprintCategory.DataQuality, fingerprint.Category);
        }

        [Fact]
        public async Task ClassifyAsync_WhenMessageIndicatesPerformance_SetsPerformance()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint(messageTemplate: "Query took longer than expected");

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: null, CancellationToken.None);

            Assert.Equal(FingerprintCategory.Performance, fingerprint.Category);
        }

        [Fact]
        public async Task ClassifyAsync_WhenWindowCountAtSpikeThreshold_SetsRecurringKnown()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint(messageTemplate: "totally unremarkable message");
            _repositoryMock
                .Setup(x => x.GetHourlyBaselineAsync(fingerprint.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FingerprintHourlyBaselineReadModel { AverageHourlyCount = 2, SampleHours = 10 });

            // threshold = AverageHourlyCount(2) * MinSpikeMultiplier(3) = 6
            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 6, problemId: null, CancellationToken.None);

            Assert.Equal(FingerprintCategory.RecurringKnown, fingerprint.Category);
            Assert.Equal(FingerprintConstants.MinSpikeMultiplier, 3);
        }

        [Fact]
        public async Task ClassifyAsync_WhenWindowCountBelowSpikeThreshold_FallsBackToLlm()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint(messageTemplate: "totally unremarkable message");
            _repositoryMock
                .Setup(x => x.GetHourlyBaselineAsync(fingerprint.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FingerprintHourlyBaselineReadModel { AverageHourlyCount = 2, SampleHours = 10 });
            _aiServiceMock
                .Setup(x => x.ClassifyAsync(fingerprint, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<FingerprintClassificationResult>.Success(new FingerprintClassificationResult
                {
                    Category = FingerprintCategory.DataQuality,
                    Confidence = 0.9,
                    Reason = "looked like bad data",
                    Source = ClassificationSource.Llm
                }));

            // windowCount(5) < threshold(6) -> falls through rules to the LLM
            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 5, problemId: null, CancellationToken.None);

            Assert.Equal(FingerprintCategory.DataQuality, fingerprint.Category);
            Assert.Equal(ClassificationSource.Llm, fingerprint.ClassifiedBy);
            _aiServiceMock.Verify(x => x.ClassifyAsync(fingerprint, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ClassifyAsync_WhenAlreadyClassified_DoesNotCallLlmAgain()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint(messageTemplate: "totally unremarkable message", category: FingerprintCategory.DataQuality);
            _repositoryMock
                .Setup(x => x.GetHourlyBaselineAsync(fingerprint.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((FingerprintHourlyBaselineReadModel?)null);

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: null, CancellationToken.None);

            Assert.Equal(FingerprintCategory.DataQuality, fingerprint.Category);
            _aiServiceMock.Verify(x => x.ClassifyAsync(It.IsAny<Fingerprint>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ClassifyAsync_WhenAiThrows_LeavesFingerprintUnclassified()
        {
            var classifier = BuildClassifier();
            var fingerprint = BuildFingerprint(messageTemplate: "totally unremarkable message");
            _repositoryMock
                .Setup(x => x.GetHourlyBaselineAsync(fingerprint.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((FingerprintHourlyBaselineReadModel?)null);
            _aiServiceMock
                .Setup(x => x.ClassifyAsync(fingerprint, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new FingerprintAiServiceException("sidecar unavailable"));

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: null, CancellationToken.None);

            Assert.Null(fingerprint.Category);
            Assert.Null(fingerprint.ClassifiedBy);
        }

        [Fact]
        public async Task ClassifyAsync_AutoFixEligible_TrueWhenAllowlistedAndNoDenylistMatch()
        {
            var classifier = BuildClassifier(allowlist: ["DATA_QUALITY"], denylist: ["Nexus.Payments"]);
            var fingerprint = BuildFingerprint(messageTemplate: "malformed payload");

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: "Nexus.Ingestion.Job!Run", CancellationToken.None);

            Assert.True(fingerprint.AutoFixEligible);
        }

        [Fact]
        public async Task ClassifyAsync_AutoFixEligible_FalseWhenProblemIdIsNull()
        {
            var classifier = BuildClassifier(allowlist: ["DATA_QUALITY"]);
            var fingerprint = BuildFingerprint(messageTemplate: "malformed payload");

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: null, CancellationToken.None);

            Assert.False(fingerprint.AutoFixEligible);
        }

        [Fact]
        public async Task ClassifyAsync_AutoFixEligible_FalseWhenDenylistMatchesEvenIfAllowlisted()
        {
            var classifier = BuildClassifier(allowlist: ["DATA_QUALITY"], denylist: ["Nexus.Payments"]);
            var fingerprint = BuildFingerprint(messageTemplate: "malformed payload");

            await classifier.ClassifyAsync(fingerprint, isNewFingerprint: false, windowOccurrenceCount: 1, problemId: "Nexus.Payments.PaymentService!Charge", CancellationToken.None);

            Assert.False(fingerprint.AutoFixEligible);
        }
    }
}
