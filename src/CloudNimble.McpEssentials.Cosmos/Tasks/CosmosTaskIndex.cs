using System.Text.Json.Serialization;

namespace CloudNimble.McpEssentials.Cosmos.Tasks
{

    /// <summary>
    /// Singleton-partition index document: task id → owner, so server-side complete
    /// can point-read the task without a current user.
    /// </summary>
    public class CosmosTaskIndex
    {

        #region Properties

        /// <summary>
        /// Gets or sets the task identifier (Cosmos item id and partition key).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the owner recorded at create.
        /// </summary>
        [JsonPropertyName("ownerUserId")]
        public string OwnerUserId { get; set; } = "";

        /// <summary>
        /// Gets or sets Cosmos per-document TTL in seconds, or <see langword="null"/> when unlimited.
        /// Matches the task document so the index row does not outlive the task.
        /// </summary>
        [JsonPropertyName("ttl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TimeToLiveSeconds { get; set; }

        #endregion

    }

}
