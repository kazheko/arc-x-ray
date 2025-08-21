using System.Text.Json.Serialization;

namespace ArcXray.Contracts.Application
{
    // Models for JSON configuration
    public class DetectionConfiguration
    {
        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }

        [JsonPropertyName("interpretationRules")]
        public InterpretationRules InterpretationRules { get; set; }

        [JsonPropertyName("checks")]
        public List<Check> Checks { get; set; }
    }
}
