using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;
using Microsoft.Azure.Cosmos;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Cosmos.Tasks
{

    /// <summary>
    /// An <see cref="IMcpTaskStorePlus"/> that persists snapshots as Cosmos documents partitioned
    /// by owner, with a task-id index for server-side complete and native document TTL.
    /// </summary>
    /// <remarks>
    /// Register as a singleton. The host owns <see cref="CosmosClient"/>.
    /// Native TTL is the drain; <see cref="McpTaskStoreBase.AutoSweep"/> defaults to
    /// <see langword="false"/>. Optional <see cref="IMcpTaskInputSignalBus"/> fans
    /// elicitation wake-ups across nodes.
    /// </remarks>
    public class CosmosMcpTaskStore : McpTaskStoreBase, IDisposable
    {

        #region Fields

        private readonly Func<CosmosClient> _getClient;
        private readonly Func<IMcpTaskInputSignalBus?> _getSignalBus;
        private readonly Func<ICosmosTaskGateway?> _getGateway;
        private ICosmosTaskGateway? _gateway;
        private IDisposable? _subscription;
        private int _subscribed;
        private int _ensured;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Cosmos database id.
        /// </summary>
        public virtual string DatabaseId { get; set; } = "mcpessentials";

        /// <summary>
        /// Gets or sets the tasks container id.
        /// </summary>
        public virtual string TasksContainerId { get; set; } = "McpTasks";

        /// <summary>
        /// Gets or sets the index container id.
        /// </summary>
        public virtual string IndexContainerId { get; set; } = "McpTaskIndex";

        /// <summary>
        /// Gets or sets the RU/s used when creating containers, or <see langword="null"/>
        /// to create without dedicated throughput (serverless).
        /// </summary>
        public virtual int? Throughput { get; set; } = 400;

        /// <summary>
        /// Gets the Cosmos client. The host owns its lifetime.
        /// </summary>
        protected CosmosClient Client => _getClient();

        /// <summary>
        /// Gets the optional cross-node bus, or <see langword="null"/> for single-instance hosts.
        /// </summary>
        protected IMcpTaskInputSignalBus? SignalBus => _getSignalBus();

        /// <summary>
        /// Gets the document gateway. Tests may register <see cref="ICosmosTaskGateway"/> in DI.
        /// </summary>
        private ICosmosTaskGateway Gateway => _gateway ??= _getGateway() ?? new CosmosTaskGateway(_getClient);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosMcpTaskStore"/> class.
        /// </summary>
        /// <param name="client">The Cosmos client. The host owns its lifetime.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="signalBus">Optional bus that fans resolved input out to other nodes.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public CosmosMcpTaskStore(CosmosClient client, Func<string?> getCurrentUserId, IMcpTaskInputSignalBus? signalBus = null)
            : this(() => client, getCurrentUserId, () => signalBus)
        {
            ArgumentNullException.ThrowIfNull(client);
        }

        /// <summary>
        /// Initializes a new instance that always uses <paramref name="userId"/>.
        /// </summary>
        /// <param name="client">The Cosmos client. The host owns its lifetime.</param>
        /// <param name="userId">The user identifier, or <see langword="null"/> to treat the process as unauthenticated.</param>
        /// <param name="signalBus">Optional bus that fans resolved input out to other nodes.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is <see langword="null"/>.</exception>
        public CosmosMcpTaskStore(CosmosClient client, string? userId, IMcpTaskInputSignalBus? signalBus = null)
            : this(client, () => userId, signalBus)
        {
        }

        /// <summary>
        /// Initializes a new instance that resolves the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="client">The Cosmos client. The host owns its lifetime.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="signalBus">Optional bus that fans resolved input out to other nodes.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public CosmosMcpTaskStore(CosmosClient client, Func<ClaimsPrincipal?> getPrincipal, IMcpTaskInputSignalBus? signalBus = null)
            : this(client, () => GetUserId(getPrincipal()), signalBus)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
        }

        /// <summary>
        /// Initializes a new instance with deferred factories so the MCP server builder can
        /// capture the store before <c>Build()</c>.
        /// </summary>
        /// <param name="getClient">Returns the Cosmos client from the built provider.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="getSignalBus">Returns the optional bus from the built provider.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getClient"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public CosmosMcpTaskStore(
            Func<CosmosClient> getClient,
            Func<string?> getCurrentUserId,
            Func<IMcpTaskInputSignalBus?>? getSignalBus = null)
            : this(getClient, getCurrentUserId, getSignalBus, gateway: null)
        {
        }

        /// <summary>
        /// Initializes a new instance with a custom document gateway (tests).
        /// </summary>
        /// <param name="getClient">Returns the Cosmos client from the built provider.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="getSignalBus">Returns the optional bus from the built provider.</param>
        /// <param name="gateway">Document I/O. When <see langword="null"/>, the Cosmos SDK adapter is used.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getClient"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        internal CosmosMcpTaskStore(
            Func<CosmosClient> getClient,
            Func<string?> getCurrentUserId,
            Func<IMcpTaskInputSignalBus?>? getSignalBus,
            ICosmosTaskGateway? gateway)
            : this(getClient, getCurrentUserId, getSignalBus, gateway is null ? null : () => gateway)
        {
        }

        /// <summary>
        /// Initializes a new instance that resolves the document gateway from the built provider.
        /// </summary>
        /// <param name="getClient">Returns the Cosmos client from the built provider.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="getSignalBus">Returns the optional bus from the built provider.</param>
        /// <param name="getGateway">Returns a test or custom gateway, or <see langword="null"/> to use the Cosmos SDK adapter.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getClient"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        internal CosmosMcpTaskStore(
            Func<CosmosClient> getClient,
            Func<string?> getCurrentUserId,
            Func<IMcpTaskInputSignalBus?>? getSignalBus,
            Func<ICosmosTaskGateway?>? getGateway)
            : base(getCurrentUserId)
        {
            ArgumentNullException.ThrowIfNull(getClient);
            _getClient = getClient;
            _getSignalBus = getSignalBus ?? (() => null);
            _getGateway = getGateway ?? (() => null);
            AutoSweep = false;
        }

        #endregion

        #region Core Methods

        /// <inheritdoc />
        protected override async Task<McpTaskInfo> CreateTaskCoreAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureContainersAsync(cancellationToken).ConfigureAwait(false);
            EnsureBusSubscription();
            var now = GetUtcNow();
            var ttl = NormalizeTimeToLive(DefaultTimeToLive);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var taskId = NewTaskId();
                var stored = new StoredMcpTask(userId, taskId, now, ttl, DefaultPollIntervalMs);
                var document = CosmosStoredMcpTask.From(stored, now);
                if (!await Gateway.TryCreateTaskAsync(document, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                try
                {
                    await Gateway.CreateIndexAsync(
                            new CosmosTaskIndex
                            {
                                Id = taskId,
                                OwnerUserId = userId,
                                TimeToLiveSeconds = document.TimeToLiveSeconds
                            },
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await Gateway.DeleteTaskAsync(taskId, userId, cancellationToken).ConfigureAwait(false);
                    throw;
                }

                return stored.Info;
            }
        }

        /// <inheritdoc />
        protected override async Task<McpTaskInfo?> GetTaskCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureContainersAsync(cancellationToken).ConfigureAwait(false);
            EnsureBusSubscription();
            var now = GetUtcNow();
            var response = await Gateway.ReadTaskAsync(taskId, userId, cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                return null;
            }

            var stored = response.Value.Document.ToStored();
            if (ShouldDiscard(stored, now))
            {
                await DeleteTaskAsync(taskId, stored.OwnerUserId, cancellationToken).ConfigureAwait(false);
                return null;
            }

            if (!IsOwnedBy(stored.OwnerUserId, userId))
            {
                return null;
            }

            var info = stored.Info;
            if (stored.IsFinished && ServeAndDrop)
            {
                await DeleteTaskAsync(taskId, stored.OwnerUserId, cancellationToken).ConfigureAwait(false);
            }

            return info;
        }

        /// <inheritdoc />
        protected override async Task<bool> SetCancelledCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureContainersAsync(cancellationToken).ConfigureAwait(false);
            EnsureBusSubscription();
            var now = GetUtcNow();
            var result = await TryApplyAsync(taskId, userId, current =>
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
            await EnsureContainersAsync(cancellationToken).ConfigureAwait(false);
            EnsureBusSubscription();
            var now = GetUtcNow();
            var result = await TryApplyAsync(taskId, userId, current =>
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
            await EnsureContainersAsync(cancellationToken).ConfigureAwait(false);
            EnsureBusSubscription();
            var now = GetUtcNow();
            var owner = await Gateway.ReadOwnerAsync(taskId, cancellationToken).ConfigureAwait(false);
            if (owner is null)
            {
                return;
            }

            await TryApplyAsync(taskId, owner, current => current.WithTry(next => next.TryComplete(result, now)), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SetFailedCoreAsync(string taskId, JsonElement error, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureContainersAsync(cancellationToken).ConfigureAwait(false);
            EnsureBusSubscription();
            var now = GetUtcNow();
            var owner = await Gateway.ReadOwnerAsync(taskId, cancellationToken).ConfigureAwait(false);
            if (owner is null)
            {
                return;
            }

            await TryApplyAsync(taskId, owner, current => current.WithTry(next => next.TryFail(error, now)), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SetInputRequestsCoreAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureContainersAsync(cancellationToken).ConfigureAwait(false);
            EnsureBusSubscription();
            var now = GetUtcNow();
            var owner = await Gateway.ReadOwnerAsync(taskId, cancellationToken).ConfigureAwait(false);
            if (owner is null)
            {
                return;
            }

            await TryApplyAsync(taskId, owner, current =>
            {
                if (!current.IsActive(now))
                {
                    return current;
                }

                return current.WithTry(next => next.TrySetInputRequests(inputRequests, now));
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Cosmos expires documents from the <c>ttl</c> property at write.
        /// This drain is a no-op.
        /// </remarks>
        protected override Task SweepExpiredCoreAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Client options that serialize documents with <see cref="CosmosSystemTextJsonSerializer"/>.
        /// </summary>
        /// <returns>
        /// Options the host should pass to <see cref="CosmosClient"/>. The Cosmos SDK default
        /// serializer is Newtonsoft; without these options, <c>JsonPropertyName</c> mapping
        /// on task documents does not apply.
        /// </returns>
        public static CosmosClientOptions DefaultClientOptions()
            => new() { Serializer = new CosmosSystemTextJsonSerializer() };

        /// <inheritdoc />
        public virtual void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applies <paramref name="mutate"/> under Cosmos ETag compare-and-replace.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="ownerUserId">The partition key.</param>
        /// <param name="mutate">Returns the next snapshot, or the current instance when unchanged.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Whether the document was missing, unchanged, or replaced.</returns>
        private async Task<ApplyResult> TryApplyAsync(
            string taskId,
            string ownerUserId,
            Func<StoredMcpTask, StoredMcpTask> mutate,
            CancellationToken cancellationToken)
        {
            var now = GetUtcNow();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var response = await Gateway.ReadTaskAsync(taskId, ownerUserId, cancellationToken).ConfigureAwait(false);
                if (response is null)
                {
                    return ApplyResult.Missing;
                }

                var current = response.Value.Document.ToStored();
                if (ShouldDiscard(current, now))
                {
                    await DeleteTaskAsync(taskId, ownerUserId, cancellationToken).ConfigureAwait(false);
                    return ApplyResult.Missing;
                }

                var next = mutate(current);
                if (ReferenceEquals(next, current))
                {
                    return ApplyResult.Unchanged;
                }

                var document = CosmosStoredMcpTask.From(next, now);
                var replaced = await Gateway.TryReplaceTaskAsync(document, response.Value.ETag, cancellationToken).ConfigureAwait(false);
                if (replaced == ApplyResult.Updated)
                {
                    return ApplyResult.Updated;
                }

                if (replaced == ApplyResult.Missing)
                {
                    return ApplyResult.Missing;
                }
            }
        }

        /// <summary>
        /// Deletes the task document and its index row.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="ownerUserId">The task partition key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task DeleteTaskAsync(string taskId, string ownerUserId, CancellationToken cancellationToken)
        {
            await Gateway.DeleteTaskAsync(taskId, ownerUserId, cancellationToken).ConfigureAwait(false);
            await Gateway.DeleteIndexAsync(taskId, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the database and containers on first use.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task EnsureContainersAsync(CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _ensured) == 1)
            {
                return;
            }

            await Gateway.EnsureAsync(DatabaseId, TasksContainerId, IndexContainerId, Throughput, cancellationToken).ConfigureAwait(false);
            Interlocked.Exchange(ref _ensured, 1);
        }

        /// <summary>
        /// Subscribes to the optional bus once.
        /// </summary>
        private void EnsureBusSubscription()
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

            _subscription = bus.Subscribe((taskId, requestId) =>
                OnInputResponseReceived(taskId, requestId, EmptyInputResponse));
        }

        #endregion

    }

}
