namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Extensions
{

    /// <summary>
    /// Host-owned row mapped by <see cref="HostOnlyDbContext"/> so tests can prove MCP mapping
    /// does not replace the host model.
    /// </summary>
    public class HostWidget
    {

        #region Properties

        /// <summary>
        /// Gets or sets the primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string Name { get; set; } = "";

        #endregion

    }

}
