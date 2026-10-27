using Microsoft.EntityFrameworkCore;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Extensions
{

    /// <summary>
    /// Host context that never maps MCP tasks itself. Used to prove
    /// <c>DbContextOptionsBuilder.AddMcpTasksTable</c> maps after <c>OnModelCreating</c>.
    /// </summary>
    public class HostOnlyDbContext : DbContext
    {

        #region Properties

        /// <summary>
        /// Gets the host-owned widget set.
        /// </summary>
        public DbSet<HostWidget> Widgets => Set<HostWidget>();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HostOnlyDbContext"/> class.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public HostOnlyDbContext(DbContextOptions<HostOnlyDbContext> options)
            : base(options)
        {
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HostWidget>().HasKey(e => e.Id);
        }

        #endregion

    }

}
