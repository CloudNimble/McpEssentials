using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using StackExchange.Redis;

namespace CloudNimble.McpEssentials.Redis.Tasks
{

    /// <summary>
    /// An <see cref="IMcpTaskStorePlus"/> that persists snapshots as Redis strings with
    /// <c>PEXPIREAT</c> from <see cref="StoredMcpTask.DiscardAfter"/> and fans resolved input
    /// over pub/sub.
    /// </summary>
    /// <remarks>
    /// Register as a singleton. The host owns <see cref="IConnectionMultiplexer"/>.
    /// Native expiry is the drain; <see cref="McpTaskStoreBase.AutoSweep"/> defaults to
    /// <see langword="false"/>. <c>WAIT</c> after create is off until
    /// <see cref="WaitReplicas"/> is positive. Eventually-consistent Redis without wait
    /// is not SEP-2663 durable across nodes.
    /// </remarks>
    public class RedisMcpTaskStore : NotifyingMcpTaskStoreBase
    {

        #region Fields

        private const string CreateScript = """
            if redis.call('EXISTS', KEYS[1]) == 1 then
              return 0
            end
            redis.call('SET', KEYS[1], ARGV[1])
            if ARGV[2] ~= '' then
              redis.call('PEXPIREAT', KEYS[1], ARGV[2])
            end
            return 1
            """;

        private const string CompareAndSwapScript = """
            local current = redis.call('GET', KEYS[1])
            if not current then
              return 0
            end
            if current ~= ARGV[1] then
              return -1
            end
            redis.call('SET', KEYS[1], ARGV[2])
            if ARGV[3] ~= '' then
              redis.call('PEXPIREAT', KEYS[1], ARGV[3])
            end
            return 1
            """;

        private readonly Func<IConnectionMultiplexer> _getMultiplexer;
        private IDatabase? _database;
        private ISubscriber? _subscriber;
        private ChannelMessageQueue? _subscription;
        private string _keyPrefixCache = "mcp";
        private string _taskKeyHead = "mcp:task:";
        private RedisChannel _inputChannel = RedisChannel.Literal("mcp:task:input");

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the key prefix. Task keys are <c>{prefix}:task:{taskId}</c>.
        /// </summary>
        public virtual string KeyPrefix { get; set; } = "mcp";

        /// <summary>
        /// Gets or sets how many replicas must acknowledge a create. Zero skips <c>WAIT</c>.
        /// </summary>
        public virtual int WaitReplicas { get; set; }

        /// <summary>
        /// Gets or sets how long to wait for replica acknowledgement.
        /// </summary>
        public virtual TimeSpan WaitTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the Redis database index, or <c>-1</c> for the multiplexer default.
        /// </summary>
        public virtual int Database { get; set; } = -1;

        /// <summary>
        /// Gets the multiplexer used for commands and pub/sub.
        /// </summary>
        protected IConnectionMultiplexer Multiplexer => _getMultiplexer();

        /// <summary>
        /// Gets the database for task keys.
        /// </summary>
        protected IDatabase Redis
        {
            get
            {
                var multiplexer = Multiplexer;
                var database = _database;
                if (database is not null && ReferenceEquals(database.Multiplexer, multiplexer))
                {
                    return database;
                }

                database = Database >= 0 ? multiplexer.GetDatabase(Database) : multiplexer.GetDatabase();
                _database = database;
                return database;
            }
        }

        /// <summary>
        /// Gets the subscriber for resolved-input pub/sub.
        /// </summary>
        protected ISubscriber Subscriber
        {
            get
            {
                var multiplexer = Multiplexer;
                var subscriber = _subscriber;
                if (subscriber is not null && ReferenceEquals(subscriber.Multiplexer, multiplexer))
                {
                    return subscriber;
                }

                subscriber = multiplexer.GetSubscriber();
                _subscriber = subscriber;
                return subscriber;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisMcpTaskStore"/> class.
        /// </summary>
        /// <param name="multiplexer">The Redis connection. The host owns its lifetime.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="multiplexer"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public RedisMcpTaskStore(IConnectionMultiplexer multiplexer, Func<string?> getCurrentUserId)
            : this(() => multiplexer, getCurrentUserId)
        {
            ArgumentNullException.ThrowIfNull(multiplexer);
        }

        /// <summary>
        /// Initializes a new instance that always uses <paramref name="userId"/>.
        /// </summary>
        /// <param name="multiplexer">The Redis connection. The host owns its lifetime.</param>
        /// <param name="userId">The user identifier, or <see langword="null"/> to treat the process as unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="multiplexer"/> is <see langword="null"/>.</exception>
        public RedisMcpTaskStore(IConnectionMultiplexer multiplexer, string? userId)
            : this(multiplexer, () => userId)
        {
        }

        /// <summary>
        /// Initializes a new instance that resolves the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="multiplexer">The Redis connection. The host owns its lifetime.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="multiplexer"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public RedisMcpTaskStore(IConnectionMultiplexer multiplexer, Func<ClaimsPrincipal?> getPrincipal)
            : this(multiplexer, () => GetUserId(getPrincipal()))
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
        }

        /// <summary>
        /// Initializes a new instance with deferred factories so the MCP server builder can
        /// capture the store before <c>Build()</c>.
        /// </summary>
        /// <param name="getMultiplexer">Returns the multiplexer from the built provider.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getMultiplexer"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public RedisMcpTaskStore(Func<IConnectionMultiplexer> getMultiplexer, Func<string?> getCurrentUserId)
            : base(getCurrentUserId)
        {
            ArgumentNullException.ThrowIfNull(getMultiplexer);
            _getMultiplexer = getMultiplexer;
            AutoSweep = false;
        }

        #endregion

        #region Core Methods

        /// <inheritdoc />
        protected override async Task<McpTaskInfo> CreateTaskCoreAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureSubscription();
            var now = GetUtcNow();
            var ttl = NormalizeTimeToLive(DefaultTimeToLive);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var taskId = NewTaskId();
                var stored = new StoredMcpTask(userId, taskId, now, ttl, DefaultPollIntervalMs);
                var json = Serialize(stored);
                var created = ScriptInt(await Redis.ScriptEvaluateAsync(
                        CreateScript,
                        [TaskKey(taskId)],
                        [json, ExpireArgument(stored.DiscardAfter)],
                        flags: CommandFlags.None)
                    .ConfigureAwait(false));

                if (created == 1)
                {
                    await WaitForReplicasAsync().ConfigureAwait(false);
                    return stored.Info;
                }
            }
        }

        /// <inheritdoc />
        protected override async Task<McpTaskInfo?> GetTaskCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureSubscription();
            var now = GetUtcNow();
            var key = TaskKey(taskId);
            var json = await Redis.StringGetAsync(key).ConfigureAwait(false);
            if (json.IsNull)
            {
                return null;
            }

            var stored = Deserialize(json!);
            if (ShouldDiscard(stored, now))
            {
                await Redis.KeyDeleteAsync(key).ConfigureAwait(false);
                return null;
            }

            if (!IsOwnedBy(stored.OwnerUserId, userId))
            {
                return null;
            }

            var info = stored.Info;
            if (stored.IsFinished && ServeAndDrop)
            {
                await Redis.KeyDeleteAsync(key).ConfigureAwait(false);
            }

            return info;
        }

        /// <inheritdoc />
        protected override async Task<bool> SetCancelledCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            EnsureSubscription();
            var now = GetUtcNow();
            await TryApplyAsync(taskId, current => current.WithTry(next => next.TryComplete(result, now)), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SetFailedCoreAsync(string taskId, JsonElement error, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureSubscription();
            var now = GetUtcNow();
            await TryApplyAsync(taskId, current => current.WithTry(next => next.TryFail(error, now)), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task SetInputRequestsCoreAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        /// <remarks>
        /// Redis expires keys from <see cref="StoredMcpTask.DiscardAfter"/> at write.
        /// This drain is a no-op. Override it when bulk criteria must differ from the stamp,
        /// and skip native expiry in that case.
        /// </remarks>
        protected override Task SweepExpiredCoreAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Returns the Redis key for <paramref name="taskId"/>.
        /// </summary>
        /// <param name="taskId">The opaque task identifier.</param>
        /// <returns>A key under <see cref="KeyPrefix"/>.</returns>
        protected virtual RedisKey TaskKey(string taskId)
        {
            RefreshPrefixCache();
            return _taskKeyHead + taskId;
        }

        /// <summary>
        /// Returns the pub/sub channel for resolved input.
        /// </summary>
        protected virtual RedisChannel InputChannel
        {
            get
            {
                RefreshPrefixCache();
                return _inputChannel;
            }
        }

        /// <summary>
        /// Compare-and-swap update of a stored snapshot using a Lua script.
        /// </summary>
        /// <param name="taskId">The task to update.</param>
        /// <param name="updater">
        /// Returns a new snapshot to store, or the input to leave it unchanged.
        /// Mutate a <see cref="StoredMcpTask.Clone"/>; do not mutate <c>current</c> in place.
        /// </param>
        /// <param name="cancellationToken">Observed before each read so a cancelled caller does not commit.</param>
        /// <returns>Whether the snapshot was missing, left unchanged, or replaced.</returns>
        protected virtual async Task<ApplyResult> TryApplyAsync(
            string taskId,
            Func<StoredMcpTask, StoredMcpTask?> updater,
            CancellationToken cancellationToken)
        {
            var now = GetUtcNow();
            var key = TaskKey(taskId);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = await Redis.StringGetAsync(key).ConfigureAwait(false);
                if (json.IsNull)
                {
                    return ApplyResult.Missing;
                }

                var current = Deserialize(json!);
                if (ShouldDiscard(current, now))
                {
                    await Redis.KeyDeleteAsync(key).ConfigureAwait(false);
                    return ApplyResult.Missing;
                }

                var next = updater(current) ?? current;
                if (ReferenceEquals(next, current))
                {
                    return ApplyResult.Unchanged;
                }

                var swapped = ScriptInt(await Redis.ScriptEvaluateAsync(
                        CompareAndSwapScript,
                        [key],
                        [json, Serialize(next), ExpireArgument(next.DiscardAfter)])
                    .ConfigureAwait(false));

                if (swapped == 1)
                {
                    return ApplyResult.Updated;
                }

                if (swapped == 0)
                {
                    return ApplyResult.Missing;
                }
            }
        }

        /// <inheritdoc />
        protected override Task PublishInputNotificationAsync(string payload, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Subscriber.PublishAsync(InputChannel, payload);
        }

        /// <inheritdoc />
        protected override void SubscribeToInputNotifications()
        {
            var queue = Subscriber.Subscribe(InputChannel);
            queue.OnMessage(OnChannelMessage);
            _subscription = queue;
        }

        /// <inheritdoc />
        protected override void UnsubscribeFromInputNotifications()
        {
            var queue = Interlocked.Exchange(ref _subscription, null);
            queue?.Unsubscribe();
        }

        /// <summary>
        /// Forwards a pub/sub payload to <see cref="NotifyingMcpTaskStoreBase.OnRemoteInputNotification"/>.
        /// </summary>
        /// <param name="message">The channel message.</param>
        private void OnChannelMessage(ChannelMessage message)
        {
            RedisValue payload = message.Message;
            if (payload.IsNullOrEmpty)
            {
                return;
            }

            OnRemoteInputNotification((string?)payload);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Rebuilds cached key heads when <see cref="KeyPrefix"/> changes.
        /// </summary>
        private void RefreshPrefixCache()
        {
            var prefix = KeyPrefix;
            if (prefix == _keyPrefixCache)
            {
                return;
            }

            _keyPrefixCache = prefix;
            _taskKeyHead = prefix + ":task:";
            _inputChannel = RedisChannel.Literal(prefix + ":task:input");
        }

        /// <summary>
        /// Serializes <paramref name="stored"/> through <see cref="RedisStoredMcpTask"/>.
        /// </summary>
        /// <param name="stored">The in-memory snapshot.</param>
        /// <returns>Camel-case JSON for the Redis string.</returns>
        private static RedisValue Serialize(StoredMcpTask stored)
            => JsonSerializer.SerializeToUtf8Bytes(
                RedisStoredMcpTask.From(stored),
                RedisMcpTasksJsonContext.Default.RedisStoredMcpTask);

        /// <summary>
        /// Deserializes a Redis string into a <see cref="StoredMcpTask"/>.
        /// </summary>
        /// <param name="json">The JSON payload.</param>
        /// <returns>A snapshot the Core <c>Try*</c> methods can mutate.</returns>
        /// <exception cref="JsonException">Thrown when the payload is empty or invalid.</exception>
        private static StoredMcpTask Deserialize(RedisValue json)
        {
            ReadOnlyMemory<byte> buffer = json;
            if (buffer.Length == 0)
            {
                throw new JsonException("Redis task payload was empty.");
            }

            var record = JsonSerializer.Deserialize(buffer.Span, RedisMcpTasksJsonContext.Default.RedisStoredMcpTask);
            if (record is null)
            {
                throw new JsonException("Redis task payload was empty.");
            }

            return record.ToStored();
        }

        /// <summary>
        /// Returns the <c>PEXPIREAT</c> argument: unix milliseconds, or empty when unlimited.
        /// </summary>
        /// <param name="discardAfter">The visibility ceiling, or <see langword="null"/>.</param>
        /// <returns>A Redis value Lua can read as <c>ARGV</c>.</returns>
        private static RedisValue ExpireArgument(DateTimeOffset? discardAfter)
            => discardAfter is { } deadline ? deadline.ToUnixTimeMilliseconds() : RedisValue.EmptyString;

        /// <summary>
        /// Converts a Lua integer into <see cref="int"/>.
        /// </summary>
        /// <param name="result">The script result.</param>
        /// <returns>The integer Redis returned.</returns>
        private static int ScriptInt(RedisResult result) => (int)result;

        /// <summary>
        /// Blocks until <see cref="WaitReplicas"/> replicas acknowledge the last write.
        /// </summary>
        /// <returns>A task that completes when <c>WAIT</c> returns or is skipped.</returns>
        private async Task WaitForReplicasAsync()
        {
            if (WaitReplicas <= 0)
            {
                return;
            }

            await Redis.ExecuteAsync("WAIT", WaitReplicas, (int)WaitTimeout.TotalMilliseconds).ConfigureAwait(false);
        }

        #endregion

    }

}
