using System.Text.Json.Serialization;

namespace ArcXray.Contracts.Application
{
    public class Metadata
    {
        [JsonPropertyName("projectType")]
        public string ProjectType { get; set; }

        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("lastUpdated")]
        public string LastUpdated { get; set; }
    }
}
