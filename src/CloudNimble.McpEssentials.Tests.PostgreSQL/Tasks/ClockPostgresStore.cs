using System;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using Npgsql;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// Postgres store whose clock is a settable <see cref="Now"/> for TTL and sweep tests.
    /// </summary>
    internal class ClockPostgresStore : PostgreSqlMcpTaskStore
    {

        #region Properties

        /// <summary>
        /// Gets or sets the instant <see cref="GetUtcNow"/> returns.
        /// </summary>
        public DateTimeOffset Now { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ClockPostgresStore"/> class.
        /// </summary>
        /// <param name="getDataSource">Returns a dummy data source.</param>
        /// <param name="userId">The current user identifier.</param>
        /// <param name="now">The starting clock value.</param>
        /// <param name="gateway">The in-memory gateway.</param>
        public ClockPostgresStore(Func<NpgsqlDataSource> getDataSource, string? userId, DateTimeOffset now, IPostgreSqlTaskGateway gateway)
            : base(getDataSource, () => userId, gateway)
        {
            Now = now;
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override DateTimeOffset GetUtcNow() => Now;

        #endregion

    }

}
