using System;
using CloudNimble.McpEssentials.EntityFrameworkCore.Infrastructure;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore
{

    /// <summary>
    /// Maps McpEssentials task rows from <c>AddDbContext</c> so the host <see cref="DbContext"/>
    /// type does not need a reference to this package.
    /// </summary>
    public static class McpEssentials_EntityFrameworkCore_DbContextOptionsBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Maps <see cref="McpTaskEntity"/> to table <c>McpTasks</c> after the host
        /// <c>OnModelCreating</c>. Call from <c>AddDbContext</c> in the application project.
        /// </summary>
        /// <param name="optionsBuilder">The options builder for the host context.</param>
        /// <returns>The same <paramref name="optionsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsBuilder"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Use this from <c>AddDbContext</c> in the application project when the
        /// <see cref="DbContext"/> type lives in a library that must not reference this package.
        /// The mapping runs after <c>OnModelCreating</c> on the same host context. Use
        /// <c>ModelBuilder.AddMcpTasksTable</c> in <c>OnModelCreating</c> when the context project
        /// already references this package. Pick one. Run <c>dotnet ef migrations add</c> with this
        /// project as the startup project so the table is included. A migration from a project that
        /// does not call this method will not see <c>McpTasks</c>.
        /// </remarks>
        public static DbContextOptionsBuilder AddMcpTasksTable(this DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.AddMcpTasksTable("McpTasks");

        /// <summary>
        /// Maps <see cref="McpTaskEntity"/> to <paramref name="tableName"/> in
        /// <paramref name="schema"/> after the host <c>OnModelCreating</c>. Call from
        /// <c>AddDbContext</c> in the application project.
        /// </summary>
        /// <param name="optionsBuilder">The options builder for the host context.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="schema">The schema, or <see langword="null"/> for the database default.</param>
        /// <returns>The same <paramref name="optionsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsBuilder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="tableName"/> is <see langword="null"/> or whitespace.
        /// </exception>
        /// <remarks>
        /// Use this from <c>AddDbContext</c> in the application project when the
        /// <see cref="DbContext"/> type lives in a library that must not reference this package.
        /// The mapping runs after <c>OnModelCreating</c> on the same host context. Use
        /// <c>ModelBuilder.AddMcpTasksTable</c> in <c>OnModelCreating</c> when the context project
        /// already references this package. Pick one. Run <c>dotnet ef migrations add</c> with this
        /// project as the startup project so the table is included. A migration from a project that
        /// does not call this method will not see <c>McpTasks</c>.
        /// </remarks>
        public static DbContextOptionsBuilder AddMcpTasksTable(
            this DbContextOptionsBuilder optionsBuilder,
            string tableName,
            string? schema = null)
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

            var extension = (optionsBuilder.Options.FindExtension<McpTasksOptionsExtension>()
                    ?? new McpTasksOptionsExtension())
                .WithTable(tableName, schema);
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
            return optionsBuilder;
        }

        /// <summary>
        /// Maps <see cref="McpTaskEntity"/> to table <c>McpTasks</c> after the host
        /// <c>OnModelCreating</c>. Call from <c>AddDbContext</c> in the application project.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/>.</typeparam>
        /// <param name="optionsBuilder">The options builder for the host context.</param>
        /// <returns>The same <paramref name="optionsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsBuilder"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Use this from <c>AddDbContext</c> in the application project when the
        /// <see cref="DbContext"/> type lives in a library that must not reference this package.
        /// The mapping runs after <c>OnModelCreating</c> on the same host context. Use
        /// <c>ModelBuilder.AddMcpTasksTable</c> in <c>OnModelCreating</c> when the context project
        /// already references this package. Pick one. Run <c>dotnet ef migrations add</c> with this
        /// project as the startup project so the table is included. A migration from a project that
        /// does not call this method will not see <c>McpTasks</c>.
        /// </remarks>
        public static DbContextOptionsBuilder<TContext> AddMcpTasksTable<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => optionsBuilder.AddMcpTasksTable("McpTasks");

        /// <summary>
        /// Maps <see cref="McpTaskEntity"/> to <paramref name="tableName"/> in
        /// <paramref name="schema"/> after the host <c>OnModelCreating</c>. Call from
        /// <c>AddDbContext</c> in the application project.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/>.</typeparam>
        /// <param name="optionsBuilder">The options builder for the host context.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="schema">The schema, or <see langword="null"/> for the database default.</param>
        /// <returns>The same <paramref name="optionsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsBuilder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="tableName"/> is <see langword="null"/> or whitespace.
        /// </exception>
        /// <remarks>
        /// Use this from <c>AddDbContext</c> in the application project when the
        /// <see cref="DbContext"/> type lives in a library that must not reference this package.
        /// The mapping runs after <c>OnModelCreating</c> on the same host context. Use
        /// <c>ModelBuilder.AddMcpTasksTable</c> in <c>OnModelCreating</c> when the context project
        /// already references this package. Pick one. Run <c>dotnet ef migrations add</c> with this
        /// project as the startup project so the table is included. A migration from a project that
        /// does not call this method will not see <c>McpTasks</c>.
        /// </remarks>
        public static DbContextOptionsBuilder<TContext> AddMcpTasksTable<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            string tableName,
            string? schema = null)
            where TContext : DbContext
        {
            ((DbContextOptionsBuilder)optionsBuilder).AddMcpTasksTable(tableName, schema);
            return optionsBuilder;
        }

        #endregion

    }

}
