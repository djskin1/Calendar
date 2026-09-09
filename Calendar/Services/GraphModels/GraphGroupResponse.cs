using System.Text.Json.Serialization;

namespace Calendar.Services.GraphModels
{
    public class GraphGroupResponse
    {
        [JsonPropertyName("value")]
        public List<GraphGroup> Value { get; set; } = new();

        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }
    }

    public class GraphGroup
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("securityEnabled")]
        public bool? SecurityEnabled { get; set; }

        [JsonPropertyName("groupTypes")]
        public List<string> GroupTypes { get; set; } = new();
    }
}
