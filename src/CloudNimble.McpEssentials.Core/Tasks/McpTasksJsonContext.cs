using System.Collections.Generic;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// Source-generated JSON for persisting <see cref="InputRequest"/> maps on <see cref="StoredMcpTask"/>.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Dictionary<string, InputRequest>))]
    [JsonSerializable(typeof(InputRequest))]
    public partial class McpTasksJsonContext : JsonSerializerContext
    {
    }

}
