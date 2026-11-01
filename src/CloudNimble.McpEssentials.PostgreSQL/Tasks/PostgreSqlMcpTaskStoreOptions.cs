using CloudNimble.McpEssentials.Core.Tasks;

namespace CloudNimble.McpEssentials.PostgreSQL.Tasks
{

    /// <summary>
    /// Registration knobs for <see cref="PostgreSqlMcpTaskStore"/>.
    /// </summary>
    public class PostgreSqlMcpTaskStoreOptions : McpTaskStoreOptions
    {

        #region Properties

        /// <summary>
        /// Gets or sets the schema that holds the tasks table.
        /// </summary>
        /// <value>The default is <c>public</c>.</value>
        public string Schema { get; set; } = "public";

        /// <summary>
        /// Gets or sets the tasks table name.
        /// </summary>
        /// <value>The default is <c>McpTasks</c>.</value>
        public string TableName { get; set; } = "McpTasks";

        /// <summary>
        /// Gets or sets the <c>LISTEN</c> / <c>NOTIFY</c> channel for resolved input.
        /// </summary>
        /// <value>The default is <c>mcp_essentials_tasks</c>.</value>
        public string ListenChannel { get; set; } = "mcp_essentials_tasks";

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void ApplyTo(McpTaskStoreBase store)
        {
            base.ApplyTo(store);
            if (store is PostgreSqlMcpTaskStore postgres)
            {
                postgres.Schema = Schema;
                postgres.TableName = TableName;
                postgres.ListenChannel = ListenChannel;
            }
        }

        #endregion

    }

}
