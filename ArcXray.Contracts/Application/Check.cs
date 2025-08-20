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

        [JsonPropertyName("expectedValue")]
        public string ExpectedValue { get; set; }

        [JsonPropertyName("pattern")]
        public string Pattern { get; set; }

        [JsonPropertyName("expectedAttributes")]
        public List<string> ExpectedAttributes { get; set; }

        [JsonPropertyName("expectedBase")]
        public string ExpectedBase { get; set; }

        [JsonPropertyName("expectedTypes")]
        public List<string> ExpectedTypes { get; set; }

        [JsonPropertyName("alternativeTargets")]
        public List<string> AlternativeTargets { get; set; }

        [JsonPropertyName("analysisType")]
        public string AnalysisType { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
