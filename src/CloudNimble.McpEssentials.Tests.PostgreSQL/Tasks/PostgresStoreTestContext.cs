using System;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using Npgsql;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// Dummy <see cref="NpgsqlDataSource"/> plus an in-memory gateway for store tests.
    /// </summary>
    internal class PostgresStoreTestContext : IDisposable
    {

        #region Properties

        /// <summary>
        /// Gets a data source that does not connect until opened.
        /// </summary>
        public NpgsqlDataSource DataSource { get; } =
            NpgsqlDataSource.Create("Host=127.0.0.1;Database=mcpessentials;Username=postgres;Password=postgres");

        /// <summary>
        /// Gets the in-memory table and notification bus.
        /// </summary>
        public InMemoryPostgreSqlTaskGateway Gateway { get; } = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a store bound to this context's gateway.
        /// </summary>
        /// <param name="getCurrentUserId">Returns the current user identifier.</param>
        /// <returns>The store.</returns>
        public PostgreSqlMcpTaskStore CreateStore(Func<string?> getCurrentUserId)
            => new(() => DataSource, getCurrentUserId, Gateway);

        /// <summary>
        /// Creates a store bound to a constant user.
        /// </summary>
        /// <param name="userId">The current user identifier.</param>
        /// <returns>The store.</returns>
        public PostgreSqlMcpTaskStore CreateStore(string? userId)
            => new(() => DataSource, () => userId, Gateway);

        /// <summary>
        /// Creates a clock-controlled store.
        /// </summary>
        /// <param name="userId">The current user identifier.</param>
        /// <param name="now">The starting clock.</param>
        /// <returns>The store.</returns>
        public ClockPostgresStore CreateClockStore(string? userId, DateTimeOffset now)
            => new(() => DataSource, userId, now, Gateway);

        /// <summary>
        /// Creates an inspectable store.
        /// </summary>
        /// <param name="getCurrentUserId">Returns the current user identifier.</param>
        /// <returns>The store.</returns>
        public InspectablePostgresStore CreateInspectable(Func<string?> getCurrentUserId)
            => new(() => DataSource, getCurrentUserId, Gateway);

        /// <inheritdoc />
        public void Dispose() => DataSource.Dispose();

        #endregion

    }

}
