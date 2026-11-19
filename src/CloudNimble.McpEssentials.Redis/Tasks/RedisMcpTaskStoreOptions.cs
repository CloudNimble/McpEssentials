using System;
using CloudNimble.McpEssentials.Core.Tasks;

namespace CloudNimble.McpEssentials.Redis.Tasks
{

    /// <summary>
    /// Registration knobs for <see cref="RedisMcpTaskStore"/>.
    /// </summary>
    public class RedisMcpTaskStoreOptions : McpTaskStoreOptions
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance with Redis defaults: no request-path sweep
        /// (<see cref="McpTaskStoreOptions.AutoSweep"/> is <see langword="false"/>).
        /// Redis expires keys from <see cref="StoredMcpTask.DiscardAfter"/>.
        /// </summary>
        public RedisMcpTaskStoreOptions()
        {
            AutoSweep = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the key prefix. Task keys are <c>{prefix}:task:{taskId}</c>.
        /// The input channel is <c>{prefix}:task:input</c>.
        /// </summary>
        /// <value>The default is <c>mcp</c>.</value>
        public string KeyPrefix { get; set; } = "mcp";

        /// <summary>
        /// Gets or sets how many replicas must acknowledge a write before
        /// <c>CreateTaskAsync</c> returns. Zero skips <c>WAIT</c> (standalone Redis).
        /// </summary>
        /// <value>The default is 0.</value>
        public int WaitReplicas { get; set; }

        /// <summary>
        /// Gets or sets how long to wait for replica acknowledgement.
        /// </summary>
        /// <value>The default is one second.</value>
        public TimeSpan WaitTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the Redis database index, or <c>-1</c> for the multiplexer default.
        /// </summary>
        /// <value>The default is <c>-1</c>.</value>
        public int Database { get; set; } = -1;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void ApplyTo(McpTaskStoreBase store)
        {
            base.ApplyTo(store);
            if (store is RedisMcpTaskStore redis)
            {
                redis.KeyPrefix = KeyPrefix;
                redis.WaitReplicas = WaitReplicas;
                redis.WaitTimeout = WaitTimeout;
                redis.Database = Database;
            }
        }

        #endregion

    }

}
