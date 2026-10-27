using System;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;

namespace Microsoft.EntityFrameworkCore
{

    /// <summary>
    /// Maps McpEssentials task rows onto a host <see cref="ModelBuilder"/>.
    /// </summary>
    public static class McpEssentials_EntityFrameworkCore_ModelBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Maps <see cref="McpTaskEntity"/> to table <c>McpTasks</c>. Call from the host
        /// <c>OnModelCreating</c>.
        /// </summary>
        /// <param name="modelBuilder">The model builder for the host context.</param>
        /// <returns>The same <paramref name="modelBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelBuilder"/> is <see langword="null"/>.</exception>
        public static ModelBuilder AddMcpTasksTable(this ModelBuilder modelBuilder)
            => modelBuilder.AddMcpTasksTable("McpTasks");

        /// <summary>
        /// Maps <see cref="McpTaskEntity"/> to <paramref name="tableName"/> in
        /// <paramref name="schema"/>. Call from the host <c>OnModelCreating</c>.
        /// </summary>
        /// <param name="modelBuilder">The model builder for the host context.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="schema">The schema, or <see langword="null"/> for the database default.</param>
        /// <returns>The same <paramref name="modelBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelBuilder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="tableName"/> is <see langword="null"/> or whitespace.
        /// </exception>
        public static ModelBuilder AddMcpTasksTable(this ModelBuilder modelBuilder, string tableName, string? schema = null)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);
            return modelBuilder.ApplyConfiguration(new McpTaskEntityConfiguration(tableName, schema));
        }

        #endregion

    }

}
