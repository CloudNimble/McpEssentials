using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// In-process table and notification double for <see cref="PostgreSqlMcpTaskStore"/> tests.
    /// </summary>
    internal class InMemoryPostgreSqlTaskGateway : IPostgreSqlTaskGateway
    {

        #region Fields

        private readonly ConcurrentDictionary<string, PostgreSqlStoredMcpTask> _rows = new(StringComparer.Ordinal);
        private event Action<string>? Notified;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the schema last passed to <see cref="EnsureAsync"/>.
        /// </summary>
        public string? EnsuredSchema { get; private set; }

        /// <summary>
        /// Gets the table last passed to <see cref="EnsureAsync"/>.
        /// </summary>
        public string? EnsuredTable { get; private set; }

        /// <summary>
        /// Gets how many times <see cref="EnsureAsync"/> ran.
        /// </summary>
        public int EnsureCalls { get; private set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public Task EnsureAsync(string schema, string table, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PostgreSqlIdentifiers.Quote(schema);
            PostgreSqlIdentifiers.Quote(table);
            EnsuredSchema = schema;
            EnsuredTable = table;
            EnsureCalls++;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> TryInsertAsync(PostgreSqlStoredMcpTask row, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_rows.TryAdd(row.TaskId, Clone(row)));
        }

        /// <inheritdoc />
        public Task<PostgreSqlStoredMcpTask?> ReadAsync(string taskId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_rows.TryGetValue(taskId, out var row) ? Clone(row) : null);
        }

        /// <inheritdoc />
        public Task<ApplyResult> TryUpdateAsync(PostgreSqlStoredMcpTask row, long expectedVersion, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_rows.TryGetValue(row.TaskId, out var current))
            {
                return Task.FromResult(ApplyResult.Missing);
            }

            if (current.RowVersion != expectedVersion)
            {
                return Task.FromResult(ApplyResult.Unchanged);
            }

            _rows[row.TaskId] = Clone(row);
            return Task.FromResult(ApplyResult.Updated);
        }

        /// <inheritdoc />
        public Task DeleteAsync(string taskId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _rows.TryRemove(taskId, out _);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var removed = 0;
            foreach (var pair in _rows)
            {
                if (pair.Value.DiscardAfter is { } deadline && deadline <= now && _rows.TryRemove(pair.Key, out _))
                {
                    removed++;
                }
            }

            return Task.FromResult(removed);
        }

        /// <inheritdoc />
        public Task NotifyAsync(string channel, string payload, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PostgreSqlIdentifiers.Validate(channel);
            Notified?.Invoke(payload);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public IDisposable Subscribe(string channel, Action<string> handler)
        {
            PostgreSqlIdentifiers.Validate(channel);
            Notified += handler;
            return new InMemoryPostgreSqlSubscription(() => Notified -= handler);
        }

        /// <summary>
        /// Forces the next update of <paramref name="taskId"/> to see a bumped version.
        /// </summary>
        /// <param name="taskId">The row to bump.</param>
        public void BumpVersion(string taskId)
        {
            if (_rows.TryGetValue(taskId, out var row))
            {
                row.RowVersion++;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Copies <paramref name="row"/> so later writes do not mutate a live dictionary value.
        /// </summary>
        private static PostgreSqlStoredMcpTask Clone(PostgreSqlStoredMcpTask row)
        {
            return new PostgreSqlStoredMcpTask
            {
                TaskId = row.TaskId,
                OwnerUserId = row.OwnerUserId,
                Status = row.Status,
                CreatedAt = row.CreatedAt,
                LastUpdatedAt = row.LastUpdatedAt,
                TimeToLive = row.TimeToLive,
                ExpiresAt = row.ExpiresAt,
                DiscardAfter = row.DiscardAfter,
                PollIntervalMs = row.PollIntervalMs,
                StatusMessage = row.StatusMessage,
                ResultJson = row.ResultJson,
                ErrorJson = row.ErrorJson,
                InputRequestsJson = row.InputRequestsJson,
                RowVersion = row.RowVersion
            };
        }

        #endregion

    }

}
