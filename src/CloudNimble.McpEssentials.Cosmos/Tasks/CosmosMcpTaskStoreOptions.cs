using System;
using CloudNimble.McpEssentials.Core.Tasks;

namespace CloudNimble.McpEssentials.Cosmos.Tasks
{

    /// <summary>
    /// Registration knobs for <see cref="CosmosMcpTaskStore"/>.
    /// </summary>
    public class CosmosMcpTaskStoreOptions : McpTaskStoreOptions
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance with Cosmos defaults: no request-path sweep
        /// (<see cref="McpTaskStoreOptions.AutoSweep"/> is <see langword="false"/>).
        /// Cosmos expires documents from the <c>ttl</c> property.
        /// </summary>
        public CosmosMcpTaskStoreOptions()
        {
            AutoSweep = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Cosmos database id.
        /// </summary>
        /// <value>The default is <c>mcpessentials</c>.</value>
        public string DatabaseId { get; set; } = "mcpessentials";

        /// <summary>
        /// Gets or sets the tasks container id. Partition key is <c>/ownerUserId</c>.
        /// </summary>
        /// <value>The default is <c>McpTasks</c>.</value>
        public string TasksContainerId { get; set; } = "McpTasks";

        /// <summary>
        /// Gets or sets the index container id. Partition key is <c>/id</c> (the task id).
        /// </summary>
        /// <value>The default is <c>McpTaskIndex</c>.</value>
        public string IndexContainerId { get; set; } = "McpTaskIndex";

        /// <summary>
        /// Gets or sets the RU/s used when <see cref="CosmosMcpTaskStore"/> creates containers.
        /// </summary>
        /// <value>The default is 400. Set to <see langword="null"/> on serverless accounts
        /// (or pre-create the containers yourself).</value>
        public int? Throughput { get; set; } = 400;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void ApplyTo(McpTaskStoreBase store)
        {
            base.ApplyTo(store);
            if (store is CosmosMcpTaskStore cosmos)
            {
                cosmos.DatabaseId = DatabaseId;
                cosmos.TasksContainerId = TasksContainerId;
                cosmos.IndexContainerId = IndexContainerId;
                cosmos.Throughput = Throughput;
            }
        }

        #endregion

    }

}
