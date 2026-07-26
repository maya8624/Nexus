using Nexus.Domain.Enums;

namespace Nexus.Application.Common
{
    public static class FingerprintCategoryWireFormat
    {
        private static readonly Dictionary<FingerprintCategory, string> ToWireMap = new()
        {
            [FingerprintCategory.DependencyFailure] = "DEPENDENCY_FAILURE",
            [FingerprintCategory.NewRegression] = "NEW_REGRESSION",
            [FingerprintCategory.RecurringKnown] = "RECURRING_KNOWN",
            [FingerprintCategory.ConfigAuth] = "CONFIG_AUTH",
            [FingerprintCategory.DataQuality] = "DATA_QUALITY",
            [FingerprintCategory.Performance] = "PERFORMANCE"
        };

        private static readonly Dictionary<string, FingerprintCategory> FromWireMap =
            ToWireMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        public static string ToWire(FingerprintCategory category) => ToWireMap[category];

        public static FingerprintCategory FromWire(string wireValue)
        {
            if (!FromWireMap.TryGetValue(wireValue, out var category))
                throw new InvalidOperationException($"Unrecognized fingerprint category wire value: '{wireValue}'.");

            return category;
        }
    }
}
