using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace CloudNimble.McpEssentials.PostgreSQL.Tasks
{

    /// <summary>
    /// Npgsql adapter. Network I/O lives here so <see cref="PostgreSqlMcpTaskStore"/>
    /// can be tested against an in-memory gateway.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class PostgreSqlTaskGateway : IPostgreSqlTaskGateway
    {

        #region Fields

        private readonly Func<NpgsqlDataSource> _getDataSource;
        private string _qualified = "\"public\".\"McpTasks\"";
        private string _insertSql = "";
        private string _readSql = "";
        private string _updateSql = "";
        private string _deleteSql = "";
        private string _deleteExpiredSql = "";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlTaskGateway"/> class.
        /// </summary>
        /// <param name="getDataSource">Returns the host-owned data source.</param>
        public PostgreSqlTaskGateway(Func<NpgsqlDataSource> getDataSource)
        {
            _getDataSource = getDataSource;
            SetTarget("public", "McpTasks");
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public async Task EnsureAsync(string schema, string table, CancellationToken cancellationToken)
        {
            SetTarget(schema, table);
            var ownerIndex = PostgreSqlIdentifiers.Quote("IX_" + table + "_OwnerUserId");
            var discardIndex = PostgreSqlIdentifiers.Quote("IX_" + table + "_DiscardAfter");
            var sql = $"""
                CREATE TABLE IF NOT EXISTS {_qualified} (
                  "TaskId" character varying(64) NOT NULL PRIMARY KEY,
                  "OwnerUserId" text NOT NULL,
                  "Status" text NOT NULL,
                  "CreatedAt" timestamptz NOT NULL,
                  "LastUpdatedAt" timestamptz NOT NULL,
                  "TimeToLiveTicks" bigint,
                  "ExpiresAt" timestamptz,
                  "DiscardAfter" timestamptz,
                  "PollIntervalMs" bigint,
                  "StatusMessage" text,
                  "ResultJson" jsonb,
                  "ErrorJson" jsonb,
                  "InputRequestsJson" jsonb,
                  "RowVersion" bigint NOT NULL DEFAULT 1
                );
                CREATE INDEX IF NOT EXISTS {ownerIndex} ON {_qualified} ("OwnerUserId");
                CREATE INDEX IF NOT EXISTS {discardIndex} ON {_qualified} ("DiscardAfter");
                """;

            await using var connection = await _getDataSource().OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> TryInsertAsync(PostgreSqlStoredMcpTask row, CancellationToken cancellationToken)
        {
            await using var connection = await _getDataSource().OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(_insertSql, connection);
            BindRow(command, row);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return affected == 1;
        }

        /// <inheritdoc />
        public async Task<PostgreSqlStoredMcpTask?> ReadAsync(string taskId, CancellationToken cancellationToken)
        {
            await using var connection = await _getDataSource().OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(_readSql, connection);
            command.Parameters.AddWithValue("taskId", taskId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return ReadRow(reader);
        }

        /// <inheritdoc />
        public async Task<ApplyResult> TryUpdateAsync(PostgreSqlStoredMcpTask row, long expectedVersion, CancellationToken cancellationToken)
        {
            await using var connection = await _getDataSource().OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(_updateSql, connection);
            BindRow(command, row);
            command.Parameters.AddWithValue("expected", expectedVersion);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (affected == 1)
            {
                return ApplyResult.Updated;
            }

            var existing = await ReadAsync(row.TaskId, cancellationToken).ConfigureAwait(false);
            return existing is null ? ApplyResult.Missing : ApplyResult.Unchanged;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string taskId, CancellationToken cancellationToken)
        {
            await using var connection = await _getDataSource().OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(_deleteSql, connection);
            command.Parameters.AddWithValue("taskId", taskId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            await using var connection = await _getDataSource().OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(_deleteExpiredSql, connection);
            command.Parameters.AddWithValue("now", now);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task NotifyAsync(string channel, string payload, CancellationToken cancellationToken)
        {
            PostgreSqlIdentifiers.Validate(channel);
            await using var connection = await _getDataSource().OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("SELECT pg_notify(@channel, @payload)", connection);
            command.Parameters.AddWithValue("channel", channel);
            command.Parameters.AddWithValue("payload", payload);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public IDisposable Subscribe(string channel, Action<string> handler)
        {
            var quoted = PostgreSqlIdentifiers.Quote(channel);
            NpgsqlConnection? listen = null;
            var subscription = new PostgreSqlNotificationSubscription(() =>
            {
                try
                {
                    listen?.Dispose();
                }
                catch (Exception)
                {
                }
            });

            _ = ListenAsync(quoted, handler, subscription, c => listen = c);
            return subscription;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Remembers the quoted schema.table used by later commands.
        /// </summary>
        /// <param name="schema">The schema name.</param>
        /// <param name="table">The table name.</param>
        private void SetTarget(string schema, string table)
        {
            _qualified = PostgreSqlIdentifiers.Quote(schema) + "." + PostgreSqlIdentifiers.Quote(table);
            _insertSql = $"""
                INSERT INTO {_qualified} (
                  "TaskId", "OwnerUserId", "Status", "CreatedAt", "LastUpdatedAt",
                  "TimeToLiveTicks", "ExpiresAt", "DiscardAfter", "PollIntervalMs",
                  "StatusMessage", "ResultJson", "ErrorJson", "InputRequestsJson", "RowVersion")
                VALUES (
                  @taskId, @owner, @status, @created, @updated,
                  @ttl, @expires, @discard, @poll,
                  @message, @result, @error, @inputs, @version)
                ON CONFLICT ("TaskId") DO NOTHING
                """;
            _readSql = $"""
                SELECT "TaskId", "OwnerUserId", "Status", "CreatedAt", "LastUpdatedAt",
                       "TimeToLiveTicks", "ExpiresAt", "DiscardAfter", "PollIntervalMs",
                       "StatusMessage", "ResultJson", "ErrorJson", "InputRequestsJson", "RowVersion"
                FROM {_qualified}
                WHERE "TaskId" = @taskId
                """;
            _updateSql = $"""
                UPDATE {_qualified} SET
                  "OwnerUserId" = @owner,
                  "Status" = @status,
                  "CreatedAt" = @created,
                  "LastUpdatedAt" = @updated,
                  "TimeToLiveTicks" = @ttl,
                  "ExpiresAt" = @expires,
                  "DiscardAfter" = @discard,
                  "PollIntervalMs" = @poll,
                  "StatusMessage" = @message,
                  "ResultJson" = @result,
                  "ErrorJson" = @error,
                  "InputRequestsJson" = @inputs,
                  "RowVersion" = @version
                WHERE "TaskId" = @taskId AND "RowVersion" = @expected
                """;
            _deleteSql = $"""DELETE FROM {_qualified} WHERE "TaskId" = @taskId""";
            _deleteExpiredSql = $"""DELETE FROM {_qualified} WHERE "DiscardAfter" IS NOT NULL AND "DiscardAfter" <= @now""";
        }

        /// <summary>
        /// LISTENs on a dedicated connection until <paramref name="subscription"/> is disposed.
        /// </summary>
        /// <param name="quotedChannel">The channel name already quoted by <see cref="PostgreSqlIdentifiers.Quote"/>.</param>
        /// <param name="handler">Invoked with the raw <c>NOTIFY</c> payload.</param>
        /// <param name="subscription">Cancels the wait loop and closes the connection.</param>
        /// <param name="capture">Receives the dedicated listen connection so dispose can close it.</param>
        private async Task ListenAsync(
            string quotedChannel,
            Action<string> handler,
            PostgreSqlNotificationSubscription subscription,
            Action<NpgsqlConnection> capture)
        {
            try
            {
                var connection = await _getDataSource().OpenConnectionAsync(subscription.Token).ConfigureAwait(false);
                capture(connection);
                connection.Notification += (_, args) => handler(args.Payload ?? "");

                await using (var listen = new NpgsqlCommand("LISTEN " + quotedChannel, connection))
                {
                    await listen.ExecuteNonQueryAsync(subscription.Token).ConfigureAwait(false);
                }

                while (!subscription.Token.IsCancellationRequested)
                {
                    await connection.WaitAsync(subscription.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Binds row columns onto <paramref name="command"/>.
        /// </summary>
        private static void BindRow(NpgsqlCommand command, PostgreSqlStoredMcpTask row)
        {
            command.Parameters.AddWithValue("taskId", row.TaskId);
            command.Parameters.AddWithValue("owner", row.OwnerUserId);
            command.Parameters.AddWithValue("status", row.Status.ToString());
            command.Parameters.AddWithValue("created", row.CreatedAt);
            command.Parameters.AddWithValue("updated", row.LastUpdatedAt);
            command.Parameters.AddWithValue("ttl", (object?)row.TimeToLive?.Ticks ?? DBNull.Value);
            command.Parameters.AddWithValue("expires", (object?)row.ExpiresAt ?? DBNull.Value);
            command.Parameters.AddWithValue("discard", (object?)row.DiscardAfter ?? DBNull.Value);
            command.Parameters.AddWithValue("poll", (object?)row.PollIntervalMs ?? DBNull.Value);
            command.Parameters.AddWithValue("message", (object?)row.StatusMessage ?? DBNull.Value);
            command.Parameters.Add(new NpgsqlParameter("result", NpgsqlDbType.Jsonb) { Value = (object?)row.ResultJson ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("error", NpgsqlDbType.Jsonb) { Value = (object?)row.ErrorJson ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("inputs", NpgsqlDbType.Jsonb) { Value = (object?)row.InputRequestsJson ?? DBNull.Value });
            command.Parameters.AddWithValue("version", row.RowVersion);
        }

        /// <summary>
        /// Materializes the current reader row.
        /// </summary>
        private static PostgreSqlStoredMcpTask ReadRow(NpgsqlDataReader reader)
        {
            return new PostgreSqlStoredMcpTask
            {
                TaskId = reader.GetString(0),
                OwnerUserId = reader.GetString(1),
                Status = Enum.Parse<McpTaskStatus>(reader.GetString(2)),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(3),
                LastUpdatedAt = reader.GetFieldValue<DateTimeOffset>(4),
                TimeToLive = reader.IsDBNull(5) ? null : TimeSpan.FromTicks(reader.GetInt64(5)),
                ExpiresAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                DiscardAfter = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                PollIntervalMs = reader.IsDBNull(8) ? null : reader.GetInt64(8),
                StatusMessage = reader.IsDBNull(9) ? null : reader.GetString(9),
                ResultJson = reader.IsDBNull(10) ? null : reader.GetString(10),
                ErrorJson = reader.IsDBNull(11) ? null : reader.GetString(11),
                InputRequestsJson = reader.IsDBNull(12) ? null : reader.GetString(12),
                RowVersion = reader.GetInt64(13)
            };
        }

        #endregion

    }

}
