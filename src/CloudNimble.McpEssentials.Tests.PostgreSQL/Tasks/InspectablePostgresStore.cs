using System;
using System.Collections.Generic;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using Npgsql;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// Exposes Postgres store internals (ids, gateway) for unit coverage.
    /// </summary>
    internal class InspectablePostgresStore : PostgreSqlMcpTaskStore
    {

        #region Properties

        /// <summary>
        /// Gets ids to return from <see cref="NewTaskId"/> before falling back to the base.
        /// </summary>
        public Queue<string> ForcedIds { get; } = new();

        /// <summary>
        /// Gets a public view of <see cref="PostgreSqlMcpTaskStore.Gateway"/>.
        /// </summary>
        public IPostgreSqlTaskGateway PublicGateway => Gateway;

        /// <summary>
        /// Gets the host data source.
        /// </summary>
        public NpgsqlDataSource PublicDataSource => DataSource;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectablePostgresStore"/> class.
        /// </summary>
        /// <param name="getDataSource">Returns a dummy data source.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier.</param>
        /// <param name="gateway">The in-memory gateway.</param>
        public InspectablePostgresStore(Func<NpgsqlDataSource> getDataSource, Func<string?> getCurrentUserId, IPostgreSqlTaskGateway gateway)
            : base(getDataSource, getCurrentUserId, gateway)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns <see cref="NewTaskId"/> for tests.
        /// </summary>
        /// <returns>A 32-character hex id, or a forced id.</returns>
        public string PublicNewTaskId() => NewTaskId();

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override string NewTaskId()
            => ForcedIds.TryDequeue(out var id) ? id : base.NewTaskId();

        #endregion

    }

}
