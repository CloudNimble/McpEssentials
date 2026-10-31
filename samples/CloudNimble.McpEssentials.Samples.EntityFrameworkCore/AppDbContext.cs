using Microsoft.EntityFrameworkCore;

namespace CloudNimble.McpEssentials.Samples.EntityFrameworkCore
{

    /// <summary>
    /// Host <see cref="DbContext"/>. MCP task rows are mapped with <c>AddMcpTasksTable</c>.
    /// </summary>
    public class AppDbContext : DbContext
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AppDbContext"/> class.
        /// </summary>
        /// <param name="options">The context options.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddMcpTasksTable();
        }

        #endregion

    }

}
