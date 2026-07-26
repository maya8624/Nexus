using Microsoft.Extensions.Options;
using Nexus.Application.Services;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class FingerprintRouterTests
    {
        private static FingerprintRouter BuildRouter(List<OwnershipRule> ownership, string defaultAssignee = "default-owner")
        {
            var settings = Options.Create(new FingerprintRoutingSettings
            {
                Ownership = ownership,
                DefaultAssignee = defaultAssignee
            });

            return new FingerprintRouter(settings);
        }

        private static Fingerprint BuildFingerprint(string? serviceName = null, string? operation = null, FingerprintCategory? category = null) 
            => new()
        {
            Id = "fp_deadbeef",
            Hash = "deadbeef",
            Level = FingerprintLevel.Error,
            MessageTemplate = "template",
            ServiceName = serviceName,
            Operation = operation,
            Category = category,
            GithubStatus = GithubIssueStatus.None,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        [Fact]
        public void Route_WhenServiceNameMatches_ReturnsAssignee()
        {
            var router = BuildRouter([
                new OwnershipRule { Match = new OwnershipMatch { ServiceName = "rag-service" }, Assignee = "team-rag" }
            ]);

            var fingerprint = BuildFingerprint(serviceName: "rag-service");

            Assert.Equal("team-rag", router.Route(fingerprint));
        }

        [Fact]
        public void Route_WhenOperationPrefixMatches_ReturnsAssignee()
        {
            var router = BuildRouter([
                new OwnershipRule { Match = new OwnershipMatch { OperationPrefix = "POST /api/internal/invoices" }, Assignee = "team-invoices" }
            ]);
            var fingerprint = BuildFingerprint(operation: "POST /api/internal/invoices/extract");

            Assert.Equal("team-invoices", router.Route(fingerprint));
        }

        [Fact]
        public void Route_WhenCategoryMatches_ReturnsAssignee()
        {
            var router = BuildRouter([
                new OwnershipRule { Match = new OwnershipMatch { Category = "DATA_QUALITY" }, Assignee = "team-data" }
            ]);
            var fingerprint = BuildFingerprint(category: FingerprintCategory.DataQuality);

            Assert.Equal("team-data", router.Route(fingerprint));
        }

        [Fact]
        public void Route_WhenOnlySomeSubMatchesHit_DoesNotMatch()
        {
            var router = BuildRouter([
                new OwnershipRule
                {
                    Match = new OwnershipMatch { ServiceName = "rag-service", Category = "DATA_QUALITY" },
                    Assignee = "team-rag"
                }
            ]);
            // ServiceName matches, but Category doesn't - the rule requires both.
            var fingerprint = BuildFingerprint(serviceName: "rag-service", category: FingerprintCategory.Performance);

            Assert.Equal("default-owner", router.Route(fingerprint));
        }

        [Fact]
        public void Route_WhenNoRuleMatches_ReturnsDefaultAssignee()
        {
            var router = BuildRouter([
                new OwnershipRule { Match = new OwnershipMatch { ServiceName = "rag-service" }, Assignee = "team-rag" }
            ]);
            var fingerprint = BuildFingerprint(serviceName: "nexus-api-dev");

            Assert.Equal("default-owner", router.Route(fingerprint));
        }

        [Fact]
        public void Route_FirstMatchWins_ReturnsFirstMatchingRuleAssignee()
        {
            var router = BuildRouter([
                new OwnershipRule { Match = new OwnershipMatch { ServiceName = "rag-service" }, Assignee = "team-rag-specific" },
                new OwnershipRule { Match = new OwnershipMatch { Category = "PERFORMANCE" }, Assignee = "team-perf-catchall" }
            ]);
            var fingerprint = BuildFingerprint(serviceName: "rag-service", category: FingerprintCategory.Performance);

            Assert.Equal("team-rag-specific", router.Route(fingerprint));
        }
    }
}
