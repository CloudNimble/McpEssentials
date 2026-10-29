namespace CloudNimble.McpEssentials.Redis.Tasks
{

    /// <summary>
    /// Pub/sub payload for a resolved input. Task id plus request id only.
    /// </summary>
    public class RedisInputSignal
    {

        #region Properties

        /// <summary>
        /// Gets or sets the task identifier.
        /// </summary>
        public string TaskId { get; set; } = "";

        /// <summary>
        /// Gets or sets the input request identifier.
        /// </summary>
        public string RequestId { get; set; } = "";

        #endregion

    }

}
