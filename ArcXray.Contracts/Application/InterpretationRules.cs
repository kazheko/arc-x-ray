using System.Text.Json.Serialization;

namespace ArcXray.Contracts.Application
{
    public class InterpretationRules
    {
        [JsonPropertyName("thresholds")]
        public List<Threshold> Thresholds { get; set; }

        [JsonPropertyName("minimumChecksForConfidence")]
        public Dictionary<string, List<string>> MinimumChecksForConfidence { get; set; }
    }
}
