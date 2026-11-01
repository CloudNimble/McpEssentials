using System.Collections.Generic;
using System.Text.Json.Serialization;
using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Redis.Tasks
{

    /// <summary>
    /// Source-generated JSON for Redis task keys.
    /// Status persists as the enum name.
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(RedisStoredMcpTask))]
    [JsonSerializable(typeof(Dictionary<string, InputRequest>))]
    [JsonSerializable(typeof(InputRequest))]
    public partial class RedisMcpTasksJsonContext : JsonSerializerContext
    {
    }

}
