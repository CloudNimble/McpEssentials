using Microsoft.EntityFrameworkCore;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// Host-style context used by EF Core store tests. Applies the package configuration only.
    /// </summary>
    public class McpTaskDbContext : DbContext
    {

        #region Constructors

        public McpTaskDbContext(DbContextOptions<McpTaskDbContext> options)
            : base(options)
        {
        }

        #endregion

        #region Protected Methods

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddMcpTasksTable();
        }

        #endregion

    }

}
