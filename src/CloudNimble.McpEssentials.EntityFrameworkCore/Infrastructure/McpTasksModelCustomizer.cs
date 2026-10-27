using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudNimble.McpEssentials.EntityFrameworkCore.Infrastructure
{

    /// <summary>
    /// Calls the host <c>OnModelCreating</c>, then maps <see cref="McpTaskEntity"/>.
    /// Registered by <see cref="McpTasksOptionsExtension"/>.
    /// </summary>
    internal class McpTasksModelCustomizer : ModelCustomizer
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTasksModelCustomizer"/> class.
        /// </summary>
        /// <param name="dependencies">EF Core customizer dependencies.</param>
        public McpTasksModelCustomizer(ModelCustomizerDependencies dependencies)
            : base(dependencies)
        {
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);

            var extension = context.GetService<IDbContextOptions>().FindExtension<McpTasksOptionsExtension>();
            var tableName = extension?.TableName ?? "McpTasks";
            var schema = extension?.Schema;
            modelBuilder.AddMcpTasksTable(tableName, schema);
        }

        #endregion

    }

}
