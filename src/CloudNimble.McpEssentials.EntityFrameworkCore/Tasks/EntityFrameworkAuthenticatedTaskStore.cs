using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// An <see cref="IPerUserMcpTaskStore"/> that persists snapshots on
    /// <c>TContext.Set&lt;McpTaskEntity&gt;()</c>. Register as a singleton. The host registers
    /// <typeparamref name="TContext"/> as scoped (standard <c>AddDbContext</c>); this store opens
    /// a scope per operation.
    /// </summary>
    /// <typeparam name="TContext">The host <see cref="DbContext"/>. Apply <see cref="McpTaskEntityConfiguration"/> in <c>OnModelCreating</c>.</typeparam>
    /// <remarks>
    /// Client-facing operations require a current user and treat another user's task as missing.
    /// Server-side completion skips the live-user check. Optional <see cref="IMcpTaskInputSignalBus"/>
    /// fans <see cref="AuthenticatedMcpTaskStoreBase.InputResponseReceived"/> out to other nodes.
    /// The SDK <c>WithTasks</c> API captures the store instance before <c>Build()</c>; use
    /// <c>WithEntityFrameworkTasks</c> so the provider is stamped on first use.
    /// </remarks>
    public class EntityFrameworkAuthenticatedTaskStore<TContext> : AuthenticatedMcpTaskStoreBase, IDisposable
        where TContext : DbContext
    {

        #region Fields

        private readonly Func<IServiceScopeFactory> _getScopeFactory;
        private readonly Func<IMcpTaskInputSignalBus?> _getSignalBus;
        private IDisposable? _subscription;
        private int _subscribed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the factory used to open a scope and resolve <typeparamref name="TContext"/>.
        /// </summary>
        protected IServiceScopeFactory ScopeFactory => _getScopeFactory();

        /// <summary>
        /// Gets the optional cross-node bus, or <see langword="null"/> for single-instance hosts.
        /// </summary>
        protected IMcpTaskInputSignalBus? SignalBus => _getSignalBus();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/> class.
        /// </summary>
        /// <param name="scopeFactory">Opens a scope per operation to resolve <typeparamref name="TContext"/>.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="signalBus">Optional bus that fans resolved input out to other nodes.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="scopeFactory"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public EntityFrameworkAuthenticatedTaskStore(
            IServiceScopeFactory scopeFactory,
            Func<string?> getCurrentUserId,
            IMcpTaskInputSignalBus? signalBus = null)
            : this(() => scopeFactory, getCurrentUserId, () => signalBus)
        {
            ArgumentNullException.ThrowIfNull(scopeFactory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/> class
        /// that always uses <paramref name="userId"/>.
        /// </summary>
        /// <param name="scopeFactory">Opens a scope per operation to resolve <typeparamref name="TContext"/>.</param>
        /// <param name="userId">The user identifier, or <see langword="null"/> to treat the process as unauthenticated.</param>
        /// <param name="signalBus">Optional bus that fans resolved input out to other nodes.</param>
        public EntityFrameworkAuthenticatedTaskStore(
            IServiceScopeFactory scopeFactory,
            string? userId,
            IMcpTaskInputSignalBus? signalBus = null)
            : this(scopeFactory, () => userId, signalBus)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/> class
        /// that resolves the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="scopeFactory">Opens a scope per operation to resolve <typeparamref name="TContext"/>.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="signalBus">Optional bus that fans resolved input out to other nodes.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="scopeFactory"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public EntityFrameworkAuthenticatedTaskStore(
            IServiceScopeFactory scopeFactory,
            Func<ClaimsPrincipal?> getPrincipal,
            IMcpTaskInputSignalBus? signalBus = null)
            : this(scopeFactory, () => GetUserId(getPrincipal()), signalBus)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/> class
        /// with deferred factories so the MCP server builder can capture the store before <c>Build()</c>.
        /// </summary>
        /// <param name="getScopeFactory">Returns the scope factory from the built provider.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="getSignalBus">Returns the optional bus from the built provider.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getScopeFactory"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public EntityFrameworkAuthenticatedTaskStore(
            Func<IServiceScopeFactory> getScopeFactory,
            Func<string?> getCurrentUserId,
            Func<IMcpTaskInputSignalBus?>? getSignalBus = null)
            : base(getCurrentUserId)
        {
            ArgumentNullException.ThrowIfNull(getScopeFactory);
            _getScopeFactory = getScopeFactory;
            _getSignalBus = getSignalBus ?? (() => null);
        }

        #endregion

        #region Core Methods

        /// <inheritdoc />
        protected override async Task<McpTaskInfo> CreateTaskCoreAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = GetUtcNow();
            EnsureBusSubscription();

            var ttl = NormalizeTimeToLive(DefaultTimeToLive);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var taskId = NewTaskId();
                var entity = new McpTaskEntity(userId, taskId, now, ttl, DefaultPollIntervalMs)
                {
                    RowVersion = NewRowVersion()
                };

                using var scope = ScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TContext>();
                db.Set<McpTaskEntity>().Add(entity);

                try
                {
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return entity.ToMcpTaskInfo();
                }
                catch (DbUpdateException)
                {
                    // Unique-key race on TaskId: mint another id and retry.
                }
            }
        }

        /// <inheritdoc />
        protected override async Task<McpTaskInfo?> GetTaskCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = GetUtcNow();
            EnsureBusSubscription();

            using var scope = ScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            var set = db.Set<McpTaskEntity>();
            var entity = await set.AsNoTracking()
                .FirstOrDefaultAsync(e => e.TaskId == taskId, cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return null;
            }

            if (ShouldDiscard(entity, now))
            {
                await DeleteByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
                return null;
            }

            if (!IsOwnedBy(entity.OwnerUserId, userId))
            {
                return null;
            }

            var info = entity.ToMcpTaskInfo();
            if (entity.IsFinished && ServeAndDrop)
            {
                await DeleteByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
            }

            return info;
        }

        /// <inheritdoc />
        protected override async Task<bool> SetCancelledCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureBusSubscription();
            var now = GetUtcNow();

            var result = await TryApplyAsync(taskId, current =>
            {
                if (!IsOwnedBy(current.OwnerUserId, userId))
                {
                    return false;
                }

                return current.TryCancel(now);
            }, cancellationToken).ConfigureAwait(false);

            return result.Result == ApplyResult.Updated;
        }

        /// <inheritdoc />
        protected override async Task ResolveInputRequestsCoreAsync(string taskId, IDictionary<string, InputResponse> inputResponses, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureBusSubscription();
            var now = GetUtcNow();

            var result = await TryApplyAsync(taskId, current =>
            {
                if (!IsOwnedBy(current.OwnerUserId, userId) || !current.IsActive(now))
                {
                    return false;
                }

                return current.TryResolveInputRequests(inputResponses, now);
            }, cancellationToken).ConfigureAwait(false);

            if (result.Result != ApplyResult.Updated)
            {
                return;
            }

            var bus = SignalBus;
            foreach (var pair in inputResponses)
            {
                OnInputResponseReceived(taskId, pair.Key, pair.Value);
                if (bus is not null)
                {
                    await bus.PublishAsync(taskId, pair.Key, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        protected override async Task SetCompletedCoreAsync(string taskId, JsonElement result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureBusSubscription();
            var now = GetUtcNow();

            await TryApplyAsync(taskId, current => current.TryComplete(result, now), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SetFailedCoreAsync(string taskId, JsonElement error, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureBusSubscription();
            var now = GetUtcNow();

            await TryApplyAsync(taskId, current => current.TryFail(error, now), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SetInputRequestsCoreAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureBusSubscription();
            var now = GetUtcNow();

            await TryApplyAsync(taskId, current => current.IsActive(now) && current.TrySetInputRequests(inputRequests, now), cancellationToken)
                .ConfigureAwait(false);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public virtual void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Generates a unique task identifier.
        /// </summary>
        protected virtual string NewTaskId() => Guid.NewGuid().ToString("N");

        /// <summary>
        /// Generates a new application-managed concurrency token.
        /// </summary>
        protected virtual byte[] NewRowVersion()
        {
            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Set-deletes rows whose <see cref="StoredMcpTask.DiscardAfter"/> has passed.
        /// That stamp matches default <see cref="AuthenticatedMcpTaskStoreBase.ShouldDiscard"/>.
        /// Override this method when bulk criteria must differ; per-row Get and mutate still
        /// call <see cref="AuthenticatedMcpTaskStoreBase.ShouldDiscard"/>.
        /// </remarks>
        protected override async Task SweepExpiredCoreAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            using var scope = ScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            await db.Set<McpTaskEntity>()
                .Where(e => e.DiscardAfter != null && e.DiscardAfter <= now)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Compare-and-swap update of a stored row. Retries when the concurrency token loses.
        /// </summary>
        /// <param name="taskId">The task to update.</param>
        /// <param name="tryMutate">
        /// Mutates the tracked entity and returns <see langword="true"/> to save,
        /// or <see langword="false"/> to leave it unchanged.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Whether the row was missing, left unchanged, or replaced.</returns>
        protected virtual async Task<(ApplyResult Result, McpTaskEntity? Entity)> TryApplyAsync(
            string taskId,
            Func<McpTaskEntity, bool> tryMutate,
            CancellationToken cancellationToken)
        {
            var now = GetUtcNow();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var scope = ScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TContext>();
                var set = db.Set<McpTaskEntity>();
                var entity = await set.FirstOrDefaultAsync(e => e.TaskId == taskId, cancellationToken)
                    .ConfigureAwait(false);

                if (entity is null)
                {
                    return (ApplyResult.Missing, null);
                }

                if (ShouldDiscard(entity, now))
                {
                    set.Remove(entity);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return (ApplyResult.Missing, null);
                }

                if (!tryMutate(entity))
                {
                    return (ApplyResult.Unchanged, entity);
                }

                entity.RowVersion = NewRowVersion();

                try
                {
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return (ApplyResult.Updated, entity);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Another writer won; reload and retry.
                }
            }
        }

        /// <summary>
        /// Subscribes to <see cref="SignalBus"/> once so other nodes can wake this process.
        /// </summary>
        protected virtual void EnsureBusSubscription()
        {
            if (Interlocked.Exchange(ref _subscribed, 1) == 1)
            {
                return;
            }

            var bus = SignalBus;
            if (bus is null)
            {
                return;
            }

            _subscription = bus.Subscribe(OnBusSignal);
        }

        /// <summary>
        /// Raises <see cref="AuthenticatedMcpTaskStoreBase.InputResponseReceived"/> from a bus payload.
        /// </summary>
        protected virtual void OnBusSignal(string taskId, string requestId)
        {
            OnInputResponseReceived(taskId, requestId, InputResponse.FromElicitResult(new ElicitResult()));
        }

        #endregion

        #region Private Methods

        private async Task DeleteByIdAsync(string taskId, CancellationToken cancellationToken)
        {
            using var scope = ScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            await db.Set<McpTaskEntity>()
                .Where(e => e.TaskId == taskId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        #endregion

    }

}
