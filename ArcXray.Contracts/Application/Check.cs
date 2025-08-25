using System.Text.Json.Serialization;

namespace ArcXray.Contracts.Application
{
    public class Check
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("target")]
        public string Target { get; set; }

        [JsonPropertyName("expectedValues")]
        public List<string> ExpectedValues { get; set; }

        [JsonPropertyName("pattern")]
        public string Pattern { get; set; }

        [JsonPropertyName("alternativeTargets")]
        public List<string> AlternativeTargets { get; set; }

        [JsonPropertyName("analysisType")]
        public string AnalysisType { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("frameworks")]
        public List<string> Frameworks { get; set; }
    }
}
