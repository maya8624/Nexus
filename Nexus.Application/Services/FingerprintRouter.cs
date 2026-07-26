using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services
{
    public class FingerprintRouter : IFingerprintRouter
    {
        private readonly FingerprintRoutingSettings _settings;

        public FingerprintRouter(IOptions<FingerprintRoutingSettings> settings)
        {
            _settings = settings.Value;
        }

        public string Route(Fingerprint fingerprint)
        {
            foreach (var rule in _settings.Ownership)
            {
                if (Matches(rule.Match, fingerprint))
                    return rule.Assignee;
            }

            return _settings.DefaultAssignee;
        }

        private static bool Matches(OwnershipMatch match, Fingerprint fingerprint)
        {
            if (match.ServiceName is not null &&
                !string.Equals(match.ServiceName, fingerprint.ServiceName, StringComparison.OrdinalIgnoreCase))
                return false;

            if (match.OperationPrefix is not null &&
                (fingerprint.Operation is null || !fingerprint.Operation.StartsWith(match.OperationPrefix, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (match.Category is not null &&
                (fingerprint.Category is null || !string.Equals(match.Category, FingerprintCategoryWireFormat.ToWire(fingerprint.Category.Value), StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }
    }
}
