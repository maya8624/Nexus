using System.Text.Json.Serialization;

namespace Nexus.Application.Dtos.Responses
{
    public class SuburbSummaryResponse
    {
        public List<SuburbProfile> Suburbs { get; set; } = [];
    }

    public class SuburbProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SuburbRents Rents { get; set; } = new();
        public string? VacancyRate { get; set; }
        public string? Trend { get; set; }
    }

    public class SuburbRents
    {
        public string? OneBedroom { get; set; }
        public string? TwoBedroom { get; set; }
        public string? ThreeBedroom { get; set; }
    }

    public class AiSuburbSummaryResponse
    {
        [JsonPropertyName("suburbs")]
        public List<AiSuburbProfile> Suburbs { get; set; } = [];
    }

    public class AiSuburbProfile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("rents")]
        public AiSuburbRents Rents { get; set; } = new();

        [JsonPropertyName("vacancy_rate")]
        public string? VacancyRate { get; set; }

        [JsonPropertyName("trend")]
        public string? Trend { get; set; }
    }

    public class AiSuburbRents
    {
        [JsonPropertyName("one_bedroom")]
        public string? OneBedroom { get; set; }

        [JsonPropertyName("two_bedroom")]
        public string? TwoBedroom { get; set; }

        [JsonPropertyName("three_bedroom")]
        public string? ThreeBedroom { get; set; }
    }
}
