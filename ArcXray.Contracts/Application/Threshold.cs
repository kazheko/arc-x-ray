using System.Text.Json.Serialization;

namespace ArcXray.Analyzers.Applications.Models
{
    public class Threshold
    {
        [JsonPropertyName("min")]
        public double Min { get; set; }

        [JsonPropertyName("max")]
        public double Max { get; set; }

        [JsonPropertyName("interpretation")]
        public string Interpretation { get; set; }

        [JsonPropertyName("confidence")]
        public string Confidence { get; set; }
    }
}
