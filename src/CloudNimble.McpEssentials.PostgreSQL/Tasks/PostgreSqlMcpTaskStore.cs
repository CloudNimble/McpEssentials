using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using Npgsql;

namespace CloudNimble.McpEssentials.PostgreSQL.Tasks
{

    /// <summary>
    /// An <see cref="IMcpTaskStorePlus"/> that persists snapshots as PostgreSQL rows with
    /// <c>LISTEN</c> / <c>NOTIFY</c> fan-out for resolved input.
    /// </summary>
    /// <remarks>
    /// Register as a singleton. The host owns <see cref="NpgsqlDataSource"/>.
    /// <see cref="McpTaskStoreBase.AutoSweep"/> defaults to <see langword="true"/>. Sweep is
    /// <c>DELETE WHERE "DiscardAfter" &lt;= now</c>. Use Entity Framework Core with
    /// <c>UseNpgsql</c> when the host already has a <c>DbContext</c>; this package exists
    /// for Npgsql/Dapper hosts and for native notification.
    /// </remarks>
    public class PostgreSqlMcpTaskStore : NotifyingMcpTaskStoreBase
    {

        #region Fields

        private static readonly Func<IPostgreSqlTaskGateway?> NoGateway = static () => null;

        private readonly Func<NpgsqlDataSource> _getDataSource;
        private readonly Func<IPostgreSqlTaskGateway?> _getGateway;
        private IPostgreSqlTaskGateway? _gateway;
        private IDisposable? _subscription;
        private int _ensured;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the schema that holds the tasks table.
        /// </summary>
        public virtual string Schema { get; set; } = "public";

        /// <summary>
        /// Gets or sets the tasks table name.
        /// </summary>
        public virtual string TableName { get; set; } = "McpTasks";

        /// <summary>
        /// Gets or sets the <c>LISTEN</c> / <c>NOTIFY</c> channel for resolved input.
        /// </summary>
        public virtual string ListenChannel { get; set; } = "mcp_essentials_tasks";

        /// <summary>
        /// Gets the data source. The host owns its lifetime.
        /// </summary>
        protected NpgsqlDataSource DataSource => _getDataSource();

        /// <summary>
        /// Gets the table gateway. Tests may register <see cref="IPostgreSqlTaskGateway"/> in DI.
        /// </summary>
        internal IPostgreSqlTaskGateway Gateway => _gateway ??= _getGateway() ?? new PostgreSqlTaskGateway(_getDataSource);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlMcpTaskStore"/> class.
        /// </summary>
        /// <param name="dataSource">The Npgsql data source. The host owns its lifetime.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataSource"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public PostgreSqlMcpTaskStore(NpgsqlDataSource dataSource, Func<string?> getCurrentUserId)
            : this(() => dataSource, getCurrentUserId)
        {
            ArgumentNullException.ThrowIfNull(dataSource);
        }

        /// <summary>
        /// Initializes a new instance that always uses <paramref name="userId"/>.
        /// </summary>
        /// <param name="dataSource">The Npgsql data source. The host owns its lifetime.</param>
        /// <param name="userId">The user identifier, or <see langword="null"/> to treat the process as unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataSource"/> is <see langword="null"/>.</exception>
        public PostgreSqlMcpTaskStore(NpgsqlDataSource dataSource, string? userId)
            : this(dataSource, () => userId)
        {
        }

        /// <summary>
        /// Initializes a new instance that resolves the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="dataSource">The Npgsql data source. The host owns its lifetime.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataSource"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public PostgreSqlMcpTaskStore(NpgsqlDataSource dataSource, Func<ClaimsPrincipal?> getPrincipal)
            : this(dataSource, () => GetUserId(getPrincipal()))
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
        }

        /// <summary>
        /// Initializes a new instance with deferred factories so the MCP server builder can
        /// capture the store before <c>Build()</c>.
        /// </summary>
        /// <param name="getDataSource">Returns the data source from the built provider.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getDataSource"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public PostgreSqlMcpTaskStore(Func<NpgsqlDataSource> getDataSource, Func<string?> getCurrentUserId)
            : this(getDataSource, getCurrentUserId, gateway: null)
        {
        }

        /// <summary>
        /// Initializes a new instance with a custom table gateway (tests).
        /// </summary>
        /// <param name="getDataSource">Returns the data source from the built provider.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="gateway">Table I/O. When <see langword="null"/>, the Npgsql adapter is used.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getDataSource"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        internal PostgreSqlMcpTaskStore(
            Func<NpgsqlDataSource> getDataSource,
            Func<string?> getCurrentUserId,
            IPostgreSqlTaskGateway? gateway)
            : this(getDataSource, getCurrentUserId, gateway is null ? null : () => gateway)
        {
        }

        /// <summary>
        /// Initializes a new instance that resolves the table gateway from the built provider.
        /// </summary>
        /// <param name="getDataSource">Returns the data source from the built provider.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="getGateway">Returns a test or custom gateway, or <see langword="null"/> to use the Npgsql adapter.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getDataSource"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        internal PostgreSqlMcpTaskStore(
            Func<NpgsqlDataSource> getDataSource,
            Func<string?> getCurrentUserId,
            Func<IPostgreSqlTaskGateway?>? getGateway)
            : base(getCurrentUserId)
        {
            ArgumentNullException.ThrowIfNull(getDataSource);
            _getDataSource = getDataSource;
            _getGateway = getGateway ?? NoGateway;
        }

        #endregion

        #region Core Methods

        /// <inheritdoc />
        protected override async Task<McpTaskInfo> CreateTaskCoreAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            EnsureSubscription();
            var now = GetUtcNow();
            var ttl = NormalizeTimeToLive(DefaultTimeToLive);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var taskId = NewTaskId();
                var stored = new StoredMcpTask(userId, taskId, now, ttl, DefaultPollIntervalMs);
                var row = PostgreSqlStoredMcpTask.From(stored);
                if (await Gateway.TryInsertAsync(row, cancellationToken).ConfigureAwait(false))
                {
                    return stored.Info;
                }
            }
        }

        /// <inheritdoc />
        protected override async Task<McpTaskInfo?> GetTaskCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            EnsureSubscription();
            var now = GetUtcNow();
            var row = await Gateway.ReadAsync(taskId, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                return null;
            }

            var stored = row.ToStored();
            if (ShouldDiscard(stored, now))
            {
                await Gateway.DeleteAsync(taskId, cancellationToken).ConfigureAwait(false);
                return null;
            }

            if (!IsOwnedBy(stored.OwnerUserId, userId))
            {
                return null;
            }

            var info = stored.Info;
            if (stored.IsFinished && ServeAndDrop)
            {
                await Gateway.DeleteAsync(taskId, cancellationToken).ConfigureAwait(false);
            }

            return info;
        }

        /// <inheritdoc />
        protected override async Task<bool> SetCancelledCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            EnsureSubscription();
            var now = GetUtcNow();
            var result = await TryApplyAsync(taskId, current =>
            {
                if (!IsOwnedBy(current.OwnerUserId, userId))
                {
                    return current;
                }

                return current.WithTry(next => next.TryCancel(now));
            }, cancellationToken).ConfigureAwait(false);

            return result == ApplyResult.Updated;
        }

        /// <inheritdoc />
        protected override async Task ResolveInputRequestsCoreAsync(string taskId, IDictionary<string, InputResponse> inputResponses, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            EnsureSubscription();
            var now = GetUtcNow();
            var result = await TryApplyAsync(taskId, current =>
            {
                if (!IsOwnedBy(current.OwnerUserId, userId) || !current.IsActive(now))
                {
                    return current;
                }

                return current.WithTry(next => next.TryResolveInputRequests(inputResponses, now));
            }, cancellationToken).ConfigureAwait(false);

            if (result != ApplyResult.Updated)
            {
                return;
            }

            await NotifyResolvedInputAsync(taskId, inputResponses, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SetCompletedCoreAsync(string taskId, JsonElement result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            EnsureSubscription();
            var now = GetUtcNow();
            await TryApplyAsync(taskId, current => current.WithTry(next => next.TryComplete(result, now)), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SetFailedCoreAsync(string taskId, JsonElement error, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            EnsureSubscription();
            var now = GetUtcNow();
            await TryApplyAsync(taskId, current => current.WithTry(next => next.TryFail(error, now)), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SetInputRequestsCoreAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            EnsureSubscription();
            var now = GetUtcNow();
            await TryApplyAsync(taskId, current =>
            {
                if (!current.IsActive(now))
                {
                    return current;
                }

                return current.WithTry(next => next.TrySetInputRequests(inputRequests, now));
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SweepExpiredCoreAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            await Gateway.DeleteExpiredAsync(now, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override Task PublishInputNotificationAsync(string payload, CancellationToken cancellationToken)
            => Gateway.NotifyAsync(ListenChannel, payload, cancellationToken);

        /// <inheritdoc />
        protected override void SubscribeToInputNotifications()
        {
            _subscription = Gateway.Subscribe(ListenChannel, OnRemoteInputNotification);
        }

        /// <inheritdoc />
        protected override void UnsubscribeFromInputNotifications()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applies <paramref name="mutate"/> under integer <c>RowVersion</c> compare-and-swap.
        /// </summary>
        /// <param name="taskId">The task to update.</param>
        /// <param name="mutate">Returns the next snapshot, or the current instance when unchanged.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Whether the row was missing, left unchanged, or replaced.</returns>
        private async Task<ApplyResult> TryApplyAsync(
            string taskId,
            Func<StoredMcpTask, StoredMcpTask> mutate,
            CancellationToken cancellationToken)
        {
            var now = GetUtcNow();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = await Gateway.ReadAsync(taskId, cancellationToken).ConfigureAwait(false);
                if (row is null)
                {
                    return ApplyResult.Missing;
                }

                var current = row.ToStored();
                if (ShouldDiscard(current, now))
                {
                    await Gateway.DeleteAsync(taskId, cancellationToken).ConfigureAwait(false);
                    return ApplyResult.Missing;
                }

                var next = mutate(current);
                if (ReferenceEquals(next, current))
                {
                    return ApplyResult.Unchanged;
                }

                var nextRow = PostgreSqlStoredMcpTask.From(next);
                nextRow.RowVersion = row.RowVersion + 1;
                var applied = await Gateway.TryUpdateAsync(nextRow, row.RowVersion, cancellationToken).ConfigureAwait(false);
                if (applied != ApplyResult.Unchanged)
                {
                    return applied;
                }
            }
        }

        /// <summary>
        /// Creates the table on first use.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task EnsureTableAsync(CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _ensured) == 1)
            {
                return;
            }

            await Gateway.EnsureAsync(Schema, TableName, cancellationToken).ConfigureAwait(false);
            Interlocked.Exchange(ref _ensured, 1);
        }

        #endregion

    }

}
