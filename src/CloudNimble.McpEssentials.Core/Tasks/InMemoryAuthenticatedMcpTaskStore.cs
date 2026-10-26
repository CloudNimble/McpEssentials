using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// An in-memory <see cref="IPerUserMcpTaskStore"/> that records the user who created each task
    /// and isolates later client-facing operations to that user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Storage is owned by this type: immutable snapshots in a <see cref="ConcurrentDictionary{TKey, TValue}"/>,
    /// compare-and-swap updates, TTL expiry on access plus a throttled sweep. It does not wrap the SDK
    /// <c>InMemoryMcpTaskStore</c>.
    /// </para>
    /// <para>
    /// Client-facing operations require a current user and treat another user's task as missing.
    /// Server-side completion skips the live-user check because the SDK runs those from a background
    /// DI scope after the originating HTTP request is gone.
    /// </para>
    /// <para>
    /// Single-process only. Register as a singleton. Subclass to change isolation, storage, or TTL.
    /// For multi-instance hosts, use a durable provider described in the authenticated task store spec.
    /// </para>
    /// </remarks>
    public class InMemoryAuthenticatedMcpTaskStore : AuthenticatedMcpTaskStoreBase
    {

        #region Fields

        private static readonly IReadOnlyDictionary<string, InputRequest> EmptyInputRequests =
            new Dictionary<string, InputRequest>(0, StringComparer.Ordinal);

        private long _lastSweepTicks;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the map of task id to stored snapshot, including owner.
        /// </summary>
        protected ConcurrentDictionary<string, StoredTask> Tasks { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the default poll interval in milliseconds for new tasks.
        /// </summary>
        /// <value>The default is 1000 milliseconds.</value>
        public virtual long DefaultPollIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the default time-to-live for new tasks, or <see langword="null"/> for unlimited.
        /// </summary>
        /// <remarks>
        /// When set to a positive value, tasks are discarded once this duration elapses from their
        /// creation. A <see langword="null"/> or non-positive value keeps tasks until the process exits.
        /// </remarks>
        public virtual TimeSpan? DefaultTimeToLive { get; set; }

        /// <summary>
        /// Gets the minimum interval between opportunistic expired-task sweeps.
        /// </summary>
        protected virtual TimeSpan SweepInterval { get; } = TimeSpan.FromSeconds(30);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryAuthenticatedMcpTaskStore"/> class.
        /// </summary>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public InMemoryAuthenticatedMcpTaskStore(Func<string?> getCurrentUserId)
            : base(getCurrentUserId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryAuthenticatedMcpTaskStore"/> class
        /// that always uses <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user identifier, or <see langword="null"/> to treat the process as unauthenticated.</param>
        public InMemoryAuthenticatedMcpTaskStore(string? userId)
            : base(userId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryAuthenticatedMcpTaskStore"/> class
        /// that resolves the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public InMemoryAuthenticatedMcpTaskStore(Func<ClaimsPrincipal?> getPrincipal)
            : base(getPrincipal)
        {
        }

        #endregion

        #region Core Methods

        /// <inheritdoc />
        protected override Task<McpTaskInfo> CreateTaskCoreAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = GetUtcNow();
            SweepExpired(now);

            var ttl = NormalizeTimeToLive(DefaultTimeToLive);
            StoredTask stored;
            string taskId;
            do
            {
                taskId = NewTaskId();
                var info = new McpTaskInfo(
                    taskId,
                    McpTaskStatus.Working,
                    now,
                    now,
                    ttl,
                    DefaultPollIntervalMs,
                    null,
                    null,
                    null,
                    EmptyInputRequests);
                stored = new StoredTask(userId, info);
            }
            while (!Tasks.TryAdd(taskId, stored));

            return Task.FromResult(stored.Info);
        }

        /// <inheritdoc />
        protected override Task<McpTaskInfo?> GetTaskCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = GetUtcNow();
            SweepExpired(now);

            if (!Tasks.TryGetValue(taskId, out var stored))
            {
                return NullTask;
            }

            if (IsExpired(stored.Info, now))
            {
                Tasks.TryRemove(taskId, out _);
                return NullTask;
            }

            if (!IsOwnedBy(stored.OwnerUserId, userId))
            {
                return NullTask;
            }

            return Task.FromResult<McpTaskInfo?>(stored.Info);
        }

        /// <inheritdoc />
        protected override Task<bool> SetCancelledCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = TryApply(taskId, current =>
            {
                if (!IsOwnedBy(current.OwnerUserId, userId) || IsTerminal(current.Info.Status))
                {
                    return current;
                }

                return current.With(Clone(current.Info, status: McpTaskStatus.Cancelled, lastUpdatedAt: GetUtcNow()));
            }, out _);

            return result == ApplyResult.Updated ? TrueTask : FalseTask;
        }

        /// <inheritdoc />
        protected override Task ResolveInputRequestsCoreAsync(string taskId, IDictionary<string, InputResponse> inputResponses, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = TryApply(taskId, current =>
            {
                if (!IsOwnedBy(current.OwnerUserId, userId) || IsTerminal(current.Info.Status))
                {
                    return current;
                }

                Dictionary<string, InputRequest>? remaining = null;
                if (current.Info.InputRequests is { Count: > 0 })
                {
                    remaining = new Dictionary<string, InputRequest>(current.Info.InputRequests, StringComparer.Ordinal);
                    foreach (var key in inputResponses.Keys)
                    {
                        remaining.Remove(key);
                    }
                }

                var nextStatus = remaining is { Count: > 0 }
                    ? McpTaskStatus.InputRequired
                    : McpTaskStatus.Working;
                var nextRequests = remaining is { Count: > 0 }
                    ? remaining
                    : EmptyInputRequests;

                return current.With(Clone(current.Info, status: nextStatus, lastUpdatedAt: GetUtcNow(), inputRequests: nextRequests));
            }, out var applied);

            if (result != ApplyResult.Updated || applied is null)
            {
                return Task.CompletedTask;
            }

            foreach (var pair in inputResponses)
            {
                OnInputResponseReceived(taskId, pair.Key, pair.Value);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task SetCompletedCoreAsync(string taskId, JsonElement result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryApply(taskId, current =>
            {
                if (IsTerminal(current.Info.Status))
                {
                    return current;
                }

                return current.With(Clone(current.Info, status: McpTaskStatus.Completed, lastUpdatedAt: GetUtcNow(), result: result));
            }, out _);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task SetFailedCoreAsync(string taskId, JsonElement error, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryApply(taskId, current =>
            {
                if (IsTerminal(current.Info.Status))
                {
                    return current;
                }

                return current.With(Clone(current.Info, status: McpTaskStatus.Failed, lastUpdatedAt: GetUtcNow(), error: error));
            }, out _);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task SetInputRequestsCoreAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryApply(taskId, current =>
            {
                if (IsTerminal(current.Info.Status))
                {
                    return current;
                }

                var merged = current.Info.InputRequests is { Count: > 0 }
                    ? new Dictionary<string, InputRequest>(current.Info.InputRequests, StringComparer.Ordinal)
                    : new Dictionary<string, InputRequest>(inputRequests.Count, StringComparer.Ordinal);

                foreach (var pair in inputRequests)
                {
                    merged[pair.Key] = pair.Value;
                }

                IReadOnlyDictionary<string, InputRequest> nextRequests = merged.Count == 0
                    ? EmptyInputRequests
                    : merged;

                return current.With(Clone(current.Info, status: McpTaskStatus.InputRequired, lastUpdatedAt: GetUtcNow(), inputRequests: nextRequests));
            }, out _);

            return Task.CompletedTask;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Returns the current UTC timestamp used for create, update, expiry, and sweep.
        /// </summary>
        protected virtual DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

        /// <summary>
        /// Generates a unique task identifier.
        /// </summary>
        protected virtual string NewTaskId() => Guid.NewGuid().ToString("N");

        /// <summary>
        /// Returns whether <paramref name="info"/> has exceeded its time-to-live.
        /// </summary>
        protected virtual bool IsExpired(McpTaskInfo info, DateTimeOffset now)
        {
            var ttl = info.TimeToLive;
            if (ttl is not { } duration || duration <= TimeSpan.Zero)
            {
                return false;
            }

            return now >= info.CreatedAt + duration;
        }

        /// <summary>
        /// Removes expired tasks when the sweep interval has elapsed.
        /// </summary>
        protected virtual void SweepExpired(DateTimeOffset now)
        {
            var intervalTicks = SweepInterval.Ticks;
            if (intervalTicks > 0)
            {
                var last = Interlocked.Read(ref _lastSweepTicks);
                if (now.UtcTicks - last < intervalTicks)
                {
                    return;
                }

                Interlocked.Exchange(ref _lastSweepTicks, now.UtcTicks);
            }

            foreach (var pair in Tasks)
            {
                if (IsExpired(pair.Value.Info, now))
                {
                    Tasks.TryRemove(pair.Key, out _);
                }
            }
        }

        /// <summary>
        /// Compare-and-swap update of a stored snapshot.
        /// </summary>
        /// <param name="taskId">The task to update.</param>
        /// <param name="updater">
        /// Returns a new snapshot to store, or <paramref name="updater"/>'s input to leave the
        /// snapshot unchanged. Returning <see langword="null"/> is treated as unchanged.
        /// </param>
        /// <param name="applied">The snapshot that won the compare-and-swap, when any.</param>
        /// <returns>Whether the snapshot was missing, left unchanged, or replaced.</returns>
        protected virtual ApplyResult TryApply(string taskId, Func<StoredTask, StoredTask?> updater, out StoredTask? applied)
        {
            var now = GetUtcNow();
            SweepExpired(now);

            while (true)
            {
                if (!Tasks.TryGetValue(taskId, out var current))
                {
                    applied = null;
                    return ApplyResult.Missing;
                }

                if (IsExpired(current.Info, now))
                {
                    Tasks.TryRemove(taskId, out _);
                    applied = null;
                    return ApplyResult.Missing;
                }

                var next = updater(current) ?? current;
                if (ReferenceEquals(next, current))
                {
                    applied = current;
                    return ApplyResult.Unchanged;
                }

                if (TryCompareAndSwap(taskId, current, next))
                {
                    applied = next;
                    return ApplyResult.Updated;
                }
            }
        }

        /// <summary>
        /// Attempts to replace <paramref name="current"/> with <paramref name="next"/> for <paramref name="taskId"/>.
        /// </summary>
        protected virtual bool TryCompareAndSwap(string taskId, StoredTask current, StoredTask next)
            => Tasks.TryUpdate(taskId, next, current);

        /// <summary>
        /// Returns a positive TTL, or <see langword="null"/> when tasks should be retained until process exit.
        /// </summary>
        protected virtual TimeSpan? NormalizeTimeToLive(TimeSpan? timeToLive)
            => timeToLive is { } duration && duration > TimeSpan.Zero ? duration : null;

        /// <summary>
        /// Creates a new <see cref="McpTaskInfo"/> snapshot from <paramref name="source"/>.
        /// </summary>
        protected virtual McpTaskInfo Clone(
            McpTaskInfo source,
            McpTaskStatus? status = null,
            DateTimeOffset? lastUpdatedAt = null,
            string? statusMessage = null,
            JsonElement? result = null,
            JsonElement? error = null,
            IReadOnlyDictionary<string, InputRequest>? inputRequests = null)
        {
            return new McpTaskInfo(
                source.TaskId,
                status ?? source.Status,
                source.CreatedAt,
                lastUpdatedAt ?? source.LastUpdatedAt,
                source.TimeToLive,
                source.PollIntervalMs,
                statusMessage ?? source.StatusMessage,
                result ?? source.Result,
                error ?? source.Error,
                inputRequests ?? source.InputRequests);
        }

        #endregion

    }

}
