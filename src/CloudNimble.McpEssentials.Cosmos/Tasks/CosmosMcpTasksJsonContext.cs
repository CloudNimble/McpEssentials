using System.Collections.Generic;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Cosmos.Tasks
{

    /// <summary>
    /// Source-generated JSON for Cosmos task documents and the task-id index.
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(CosmosStoredMcpTask))]
    [JsonSerializable(typeof(CosmosTaskIndex))]
    [JsonSerializable(typeof(Dictionary<string, InputRequest>))]
    [JsonSerializable(typeof(InputRequest))]
    public partial class CosmosMcpTasksJsonContext : JsonSerializerContext
    {
    }

}
