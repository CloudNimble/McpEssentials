using System;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CloudNimble.McpEssentials.EntityFrameworkCore.Infrastructure
{

    /// <summary>
    /// EF Core options that map <see cref="McpTaskEntity"/> after the host <c>OnModelCreating</c>.
    /// Stamped by <c>DbContextOptionsBuilder.AddMcpTasksTable</c>.
    /// </summary>
    internal class McpTasksOptionsExtension : IDbContextOptionsExtension
    {

        #region Properties

        /// <summary>
        /// Gets the table name. The default is <c>McpTasks</c>.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Gets the schema, or <see langword="null"/> for the database default.
        /// </summary>
        public string? Schema { get; }

        /// <inheritdoc />
        public DbContextOptionsExtensionInfo Info { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance that maps to table <c>McpTasks</c>.
        /// </summary>
        public McpTasksOptionsExtension()
            : this("McpTasks", null)
        {
        }

        /// <summary>
        /// Initializes a new instance that maps to <paramref name="tableName"/> in
        /// <paramref name="schema"/>.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="schema">The schema, or <see langword="null"/> for the database default.</param>
        public McpTasksOptionsExtension(string tableName, string? schema)
        {
            TableName = tableName;
            Schema = string.IsNullOrWhiteSpace(schema) ? null : schema;
            Info = new McpTasksOptionsExtensionInfo(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a copy that maps to <paramref name="tableName"/> in <paramref name="schema"/>.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="schema">The schema, or <see langword="null"/> for the database default.</param>
        /// <returns>A new extension with those names.</returns>
        public virtual McpTasksOptionsExtension WithTable(string tableName, string? schema)
            => new(tableName, schema);

        /// <inheritdoc />
        public virtual void ApplyServices(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddSingleton<IModelCustomizer>(sp =>
                new McpTasksModelCustomizer(sp.GetRequiredService<ModelCustomizerDependencies>()));
            services.AddSingleton<IModelCacheKeyFactory, McpTasksModelCacheKeyFactory>();
        }

        /// <inheritdoc />
        public virtual void Validate(IDbContextOptions options)
        {
        }

        #endregion

    }

}
