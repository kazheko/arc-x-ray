using System.Text.Json.Serialization;

namespace ArcXray.Analyzers.Applications.Models
{
    public class InterpretationRules
    {
        [JsonPropertyName("thresholds")]
        public List<Threshold> Thresholds { get; set; }

        [JsonPropertyName("minimumChecksForConfidence")]
        public Dictionary<string, List<string>> MinimumChecksForConfidence { get; set; }
    }
}
