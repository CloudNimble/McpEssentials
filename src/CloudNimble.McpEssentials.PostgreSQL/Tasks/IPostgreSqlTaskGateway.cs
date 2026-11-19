using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;

namespace CloudNimble.McpEssentials.PostgreSQL.Tasks
{

    /// <summary>
    /// Table and <c>NOTIFY</c> I/O for <see cref="PostgreSqlMcpTaskStore"/>. Production talks
    /// to Postgres; tests supply an in-memory implementation so store logic runs without a server.
    /// </summary>
    internal interface IPostgreSqlTaskGateway
    {

        /// <summary>
        /// Creates the tasks table when it is missing.
        /// </summary>
        /// <param name="schema">The schema name.</param>
        /// <param name="table">The table name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task EnsureAsync(string schema, string table, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts a row. Returns <see langword="false"/> on primary-key conflict.
        /// </summary>
        /// <param name="row">The row to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see langword="true"/> when the row was inserted.</returns>
        Task<bool> TryInsertAsync(PostgreSqlStoredMcpTask row, CancellationToken cancellationToken);

        /// <summary>
        /// Reads a row by task id.
        /// </summary>
        /// <param name="taskId">The primary key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The row, or <see langword="null"/> when missing.</returns>
        Task<PostgreSqlStoredMcpTask?> ReadAsync(string taskId, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces the row when <paramref name="expectedVersion"/> matches.
        /// </summary>
        /// <param name="row">The next snapshot, including the incremented <see cref="PostgreSqlStoredMcpTask.RowVersion"/>.</param>
        /// <param name="expectedVersion">The version last read.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// <see cref="ApplyResult.Updated"/> when replaced,
        /// <see cref="ApplyResult.Missing"/> when the row is gone,
        /// <see cref="ApplyResult.Unchanged"/> on version conflict (retry).
        /// </returns>
        Task<ApplyResult> TryUpdateAsync(PostgreSqlStoredMcpTask row, long expectedVersion, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a row, ignoring not-found.
        /// </summary>
        /// <param name="taskId">The primary key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteAsync(string taskId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes rows whose <see cref="PostgreSqlStoredMcpTask.DiscardAfter"/> is at or before <paramref name="now"/>.
        /// </summary>
        /// <param name="now">The current UTC time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of rows deleted.</returns>
        Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);

        /// <summary>
        /// Publishes a resolved-input payload on <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">The <c>NOTIFY</c> channel.</param>
        /// <param name="payload"><see cref="McpTaskInputSignal.Format"/> of the task id and request id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task NotifyAsync(string channel, string payload, CancellationToken cancellationToken);

        /// <summary>
        /// Subscribes to resolved-input payloads on <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">The <c>LISTEN</c> channel.</param>
        /// <param name="handler">Invoked with the raw channel payload.</param>
        /// <returns>A subscription that stops delivery when disposed.</returns>
        IDisposable Subscribe(string channel, Action<string> handler);

    }

}
