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
    /// An in-memory <see cref="IMcpTaskStorePlus"/> that records the user who created each task
    /// and isolates later client-facing operations to that user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Storage is owned by this type: <see cref="StoredMcpTask"/> snapshots in a
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/>, compare-and-swap updates, two-window TTL,
    /// serve-and-drop on a terminal get, plus a throttled sweep. It does not wrap the SDK
    /// <c>InMemoryMcpTaskStore</c>.
    /// </para>
    /// <para>
    /// Tasks created with a <see cref="McpTaskStoreBase.DefaultTimeToLive"/> use two
    /// clocks (as permitted by SEP-2663): a work window of <c>CreatedAt + TimeToLive</c> during which
    /// input and progress are accepted, and a tracking window that keeps the snapshot visible until
    /// <c>CreatedAt + 2×TimeToLive</c> so a late complete, fail, or cancel can still be recorded and
    /// polled. That ceiling is stamped as <see cref="StoredMcpTask.DiscardAfter"/> at write time.
    /// An owner <c>GetTaskAsync</c> of a terminal snapshot serves it and then drops the row. An
    /// opportunistic throttled sweep reclaims snapshots nobody collected. Tasks created without a
    /// time-to-live are retained until a terminal get by the owner or until the process exits.
    /// </para>
    /// <para>
    /// Client-facing operations require a current user and treat another user's task as missing.
    /// Another user's get does not serve-and-drop the owner's row. Server-side completion skips the
    /// live-user check because the SDK runs those from a background DI scope after the originating
    /// HTTP request is gone.
    /// </para>
    /// <para>
    /// Single-process only. Register as a singleton. Subclass to change isolation, storage, or TTL.
    /// For multi-instance hosts, use a durable provider described in the authenticated task store spec.
    /// </para>
    /// </remarks>
    public class InMemoryMcpTaskStorePlus : McpTaskStoreBase
    {

        #region Properties

        /// <summary>
        /// Gets the map of task id to stored snapshot, including owner.
        /// </summary>
        protected ConcurrentDictionary<string, StoredMcpTask> Tasks { get; } = new(StringComparer.Ordinal);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryMcpTaskStorePlus"/> class.
        /// </summary>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public InMemoryMcpTaskStorePlus(Func<string?> getCurrentUserId)
            : base(getCurrentUserId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryMcpTaskStorePlus"/> class
        /// that always uses <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user identifier, or <see langword="null"/> to treat the process as unauthenticated.</param>
        public InMemoryMcpTaskStorePlus(string? userId)
            : base(userId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryMcpTaskStorePlus"/> class
        /// that resolves the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public InMemoryMcpTaskStorePlus(Func<ClaimsPrincipal?> getPrincipal)
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

            var ttl = NormalizeTimeToLive(DefaultTimeToLive);
            StoredMcpTask stored;
            string taskId;
            do
            {
                taskId = NewTaskId();
                stored = new StoredMcpTask(userId, taskId, now, ttl, DefaultPollIntervalMs);
            }
            while (!Tasks.TryAdd(taskId, stored));

            return Task.FromResult(stored.Info);
        }

        /// <inheritdoc />
        protected override Task<McpTaskInfo?> GetTaskCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = GetUtcNow();

            if (!Tasks.TryGetValue(taskId, out var stored))
            {
                return NullTask;
            }

            if (ShouldDiscard(stored, now))
            {
                Tasks.TryRemove(taskId, out _);
                return NullTask;
            }

            if (!IsOwnedBy(stored.OwnerUserId, userId))
            {
                return NullTask;
            }

            var info = stored.Info;
            if (stored.IsFinished && ServeAndDrop)
            {
                // The C# client polls tasks/get until it sees a terminal snapshot, then stops.
                // Serving that snapshot and dropping the row is the primary prune. Another user's
                // get must not drop the owner's row; that path returned above.
                Tasks.TryRemove(taskId, out _);
            }

            return Task.FromResult<McpTaskInfo?>(info);
        }

        /// <inheritdoc />
        protected override Task<bool> SetCancelledCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = GetUtcNow();
            var result = TryApply(taskId, current =>
            {
                if (!IsOwnedBy(current.OwnerUserId, userId))
                {
                    return current;
                }

                var next = current.Clone();
                return next.TryCancel(now) ? next : current;
            }, out _, cancellationToken);

            return result == ApplyResult.Updated ? TrueTask : FalseTask;
        }

        /// <inheritdoc />
        protected override Task ResolveInputRequestsCoreAsync(string taskId, IDictionary<string, InputResponse> inputResponses, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = GetUtcNow();
            var result = TryApply(taskId, current =>
            {
                if (!IsOwnedBy(current.OwnerUserId, userId) || !current.IsActive(now))
                {
                    return current;
                }

                var next = current.Clone();
                return next.TryResolveInputRequests(inputResponses, now) ? next : current;
            }, out var applied, cancellationToken);

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
            var now = GetUtcNow();
            TryApply(taskId, current =>
            {
                var next = current.Clone();
                return next.TryComplete(result, now) ? next : current;
            }, out _, cancellationToken);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task SetFailedCoreAsync(string taskId, JsonElement error, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = GetUtcNow();
            TryApply(taskId, current =>
            {
                var next = current.Clone();
                return next.TryFail(error, now) ? next : current;
            }, out _, cancellationToken);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task SetInputRequestsCoreAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = GetUtcNow();
            TryApply(taskId, current =>
            {
                if (!current.IsActive(now))
                {
                    return current;
                }

                var next = current.Clone();
                return next.TrySetInputRequests(inputRequests, now) ? next : current;
            }, out _, cancellationToken);

            return Task.CompletedTask;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Generates a unique task identifier.
        /// </summary>
        protected virtual string NewTaskId() => Guid.NewGuid().ToString("N");

        /// <inheritdoc />
        protected override Task SweepExpiredCoreAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            foreach (var pair in Tasks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ShouldDiscard(pair.Value, now))
                {
                    Tasks.TryRemove(pair.Key, out _);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Compare-and-swap update of a stored snapshot.
        /// </summary>
        /// <param name="taskId">The task to update.</param>
        /// <param name="updater">
        /// Returns a new snapshot to store, or <paramref name="updater"/>'s input to leave the
        /// snapshot unchanged. Returning <see langword="null"/> is treated as unchanged.
        /// Mutate a <see cref="StoredMcpTask.Clone"/>; do not mutate <c>current</c> in place.
        /// </param>
        /// <param name="applied">The snapshot that won the compare-and-swap, when any.</param>
        /// <param name="cancellationToken">
        /// Observed before each read so a cancelled caller does not commit.
        /// </param>
        /// <returns>Whether the snapshot was missing, left unchanged, or replaced.</returns>
        /// <exception cref="OperationCanceledException">
        /// <paramref name="cancellationToken"/> was cancelled before a commit.
        /// </exception>
        protected virtual ApplyResult TryApply(string taskId, Func<StoredMcpTask, StoredMcpTask?> updater, out StoredMcpTask? applied, CancellationToken cancellationToken = default)
        {
            var now = GetUtcNow();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Tasks.TryGetValue(taskId, out var current))
                {
                    applied = null;
                    return ApplyResult.Missing;
                }

                if (ShouldDiscard(current, now))
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
        protected virtual bool TryCompareAndSwap(string taskId, StoredMcpTask current, StoredMcpTask next)
            => Tasks.TryUpdate(taskId, next, current);

        #endregion

    }

}
