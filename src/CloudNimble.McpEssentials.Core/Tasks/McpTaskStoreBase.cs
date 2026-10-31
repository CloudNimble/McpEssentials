using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// Base type for authenticated MCP task stores. Wires the current-user callback, fail-closed
    /// identity, <see cref="InputResponseReceived"/>, flush pulses, and the mapping from
    /// <see cref="IMcpTaskStore"/> / <see cref="IMcpTaskStorePlus"/> into per-user core methods.
    /// </summary>
    /// <remarks>
    /// Subclass and implement the <c>Core</c> methods plus <see cref="SweepExpiredCoreAsync"/> to
    /// persist snapshots. Create inserts then pulses a throttled flush; get pulses then reads.
    /// Set <see cref="AutoSweep"/> to <see langword="false"/> and call <see cref="SweepExpiredAsync"/>
    /// from your own job to own the schedule. Override <see cref="ShouldDiscard"/> to change keep
    /// rules. Do not wrap the SDK <c>InMemoryMcpTaskStore</c>; each provider owns its storage.
    /// </remarks>
    public abstract class McpTaskStoreBase : IMcpTaskStorePlus
    {

        #region Fields

        /// <summary>
        /// A completed task that returns <see langword="null"/>.
        /// </summary>
        protected static readonly Task<McpTaskInfo?> NullTask = Task.FromResult<McpTaskInfo?>(null);

        /// <summary>
        /// A completed task that returns <see langword="false"/>.
        /// </summary>
        protected static readonly Task<bool> FalseTask = Task.FromResult(false);

        /// <summary>
        /// A completed task that returns <see langword="true"/>.
        /// </summary>
        protected static readonly Task<bool> TrueTask = Task.FromResult(true);

        /// <summary>
        /// Empty elicitation response used when a bus or pub/sub payload only carries ids.
        /// </summary>
        protected static readonly InputResponse EmptyInputResponse =
            InputResponse.FromElicitResult(new ElicitResult());

        private long _lastSweepTicks;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the callback used by the <see cref="IMcpTaskStore"/> methods to resolve the current user.
        /// </summary>
        protected Func<string?> GetCurrentUserId { get; }

        /// <summary>
        /// Gets or sets the default poll interval in milliseconds for new tasks.
        /// </summary>
        /// <value>The default is 1000 milliseconds.</value>
        public virtual long DefaultPollIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the default time-to-live for new tasks, or <see langword="null"/> for unlimited.
        /// </summary>
        /// <remarks>
        /// When set to a positive value, the work window ends at <c>CreatedAt + TimeToLive</c> and
        /// the row is discarded at <c>CreatedAt + 2×TimeToLive</c> (stamped as
        /// <see cref="StoredMcpTask.DiscardAfter"/>), or sooner when a terminal
        /// <c>GetTaskAsync</c> serves it. A <see langword="null"/> or non-positive value keeps tasks
        /// until a terminal get or until the store discards them.
        /// </remarks>
        public virtual TimeSpan? DefaultTimeToLive { get; set; }

        /// <summary>
        /// Gets or sets whether the store pulses a throttled flush after create and on get.
        /// </summary>
        /// <value>The default is <see langword="true"/>.</value>
        /// <remarks>
        /// Set to <see langword="false"/> and call <see cref="SweepExpiredAsync"/> from your own job
        /// when you want to own the schedule. The public flush handle always runs a full pass.
        /// </remarks>
        public virtual bool AutoSweep { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum interval between automatic flush pulses.
        /// </summary>
        /// <value>The default is 30 seconds. <see cref="TimeSpan.Zero"/> skips the throttle.</value>
        public virtual TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether an owner get of a terminal snapshot serves it and then drops the row.
        /// </summary>
        /// <value>The default is <see langword="true"/>.</value>
        public virtual bool ServeAndDrop { get; set; } = true;

        /// <summary>
        /// Gets or sets the UTC ticks of the last automatic flush pulse.
        /// Tests reset this to race the throttle.
        /// </summary>
        protected long LastSweepTicks
        {
            get => Interlocked.Read(ref _lastSweepTicks);
            set => Interlocked.Exchange(ref _lastSweepTicks, value);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTaskStoreBase"/> class.
        /// </summary>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        protected McpTaskStoreBase(Func<string?> getCurrentUserId)
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            GetCurrentUserId = getCurrentUserId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTaskStoreBase"/> class
        /// that always uses <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user identifier, or <see langword="null"/> to treat the process as unauthenticated.</param>
        protected McpTaskStoreBase(string? userId)
            : this(() => userId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTaskStoreBase"/> class
        /// that resolves the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        protected McpTaskStoreBase(Func<ClaimsPrincipal?> getPrincipal)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            GetCurrentUserId = () => GetUserId(getPrincipal());
        }

        #endregion

        #region Events

        /// <inheritdoc />
        public virtual event Action<InputResponseReceivedEventArgs>? InputResponseReceived;

        #endregion

        #region IMcpTaskStore

        /// <inheritdoc />
        public virtual async Task<McpTaskInfo> CreateTaskAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var created = await CreateTaskCoreAsync(RequireUserId(), cancellationToken).ConfigureAwait(false);
            await PulseSweepAsync(GetUtcNow(), cancellationToken).ConfigureAwait(false);
            return created;
        }

        /// <inheritdoc />
        public virtual async Task<McpTaskInfo?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            await PulseSweepAsync(GetUtcNow(), cancellationToken).ConfigureAwait(false);
            return await GetTaskCoreAsync(taskId, RequireUserId(), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual Task SetCompletedAsync(string taskId, JsonElement result, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            return SetCompletedCoreAsync(taskId, result, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task SetFailedAsync(string taskId, JsonElement error, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            return SetFailedCoreAsync(taskId, error, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<bool> SetCancelledAsync(string taskId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            return SetCancelledCoreAsync(taskId, RequireUserId(), cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task ResolveInputRequestsAsync(string taskId, IDictionary<string, InputResponse> inputResponses, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            ArgumentNullException.ThrowIfNull(inputResponses);
            return ResolveInputRequestsCoreAsync(taskId, inputResponses, RequireUserId(), cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task SetInputRequestsAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            ArgumentNullException.ThrowIfNull(inputRequests);
            return SetInputRequestsCoreAsync(taskId, inputRequests, cancellationToken);
        }

        #endregion

        #region IMcpTaskStorePlus

        /// <inheritdoc />
        public virtual async Task<McpTaskInfo> CreateTaskAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var created = await CreateTaskCoreAsync(RequireUserId(user), cancellationToken).ConfigureAwait(false);
            await PulseSweepAsync(GetUtcNow(), cancellationToken).ConfigureAwait(false);
            return created;
        }

        /// <inheritdoc />
        public virtual async Task<McpTaskInfo?> GetTaskAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            await PulseSweepAsync(GetUtcNow(), cancellationToken).ConfigureAwait(false);
            return await GetTaskCoreAsync(taskId, RequireUserId(user), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual Task SweepExpiredAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SweepExpiredCoreAsync(GetUtcNow(), cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<bool> SetCancelledAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            return SetCancelledCoreAsync(taskId, RequireUserId(user), cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task ResolveInputRequestsAsync(string taskId, IDictionary<string, InputResponse> inputResponses, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            ArgumentNullException.ThrowIfNull(inputResponses);
            return ResolveInputRequestsCoreAsync(taskId, inputResponses, RequireUserId(user), cancellationToken);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates a task owned by <paramref name="userId"/>.
        /// </summary>
        protected abstract Task<McpTaskInfo> CreateTaskCoreAsync(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the task when <paramref name="userId"/> owns it; otherwise <see langword="null"/>.
        /// </summary>
        protected abstract Task<McpTaskInfo?> GetTaskCoreAsync(string taskId, string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Cancels the task when <paramref name="userId"/> owns it and it is not terminal.
        /// </summary>
        protected abstract Task<bool> SetCancelledCoreAsync(string taskId, string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Resolves input requests when <paramref name="userId"/> owns the task.
        /// </summary>
        protected abstract Task ResolveInputRequestsCoreAsync(string taskId, IDictionary<string, InputResponse> inputResponses, string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Completes the task. Skips the live-user check.
        /// </summary>
        protected abstract Task SetCompletedCoreAsync(string taskId, JsonElement result, CancellationToken cancellationToken);

        /// <summary>
        /// Fails the task. Skips the live-user check.
        /// </summary>
        protected abstract Task SetFailedCoreAsync(string taskId, JsonElement error, CancellationToken cancellationToken);

        /// <summary>
        /// Merges input requests onto the task. Skips the live-user check.
        /// </summary>
        protected abstract Task SetInputRequestsCoreAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes rows that have passed their discard time.
        /// </summary>
        /// <param name="now">The current UTC time used for discard checks.</param>
        /// <param name="cancellationToken">
        /// Observed during the pass so a cancelled caller does not finish the walk.
        /// </param>
        /// <returns>A task that completes when the pass finishes.</returns>
        /// <exception cref="OperationCanceledException">
        /// <paramref name="cancellationToken"/> was cancelled during the pass.
        /// </exception>
        protected abstract Task SweepExpiredCoreAsync(DateTimeOffset now, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the current UTC timestamp used for create, update, expiry, and sweep.
        /// </summary>
        protected virtual DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

        /// <summary>
        /// Generates a unique task identifier.
        /// </summary>
        /// <returns>A 32-character hex id.</returns>
        protected virtual string NewTaskId() => Guid.NewGuid().ToString("N");

        /// <summary>
        /// Returns a positive TTL, or <see langword="null"/> when tasks should be retained until discarded.
        /// </summary>
        protected virtual TimeSpan? NormalizeTimeToLive(TimeSpan? timeToLive)
            => StoredMcpTask.NormalizeTimeToLive(timeToLive);

        /// <summary>
        /// Returns whether <paramref name="task"/> should be discarded at <paramref name="now"/>.
        /// </summary>
        /// <param name="task">The snapshot under consideration.</param>
        /// <param name="now">The current UTC time.</param>
        /// <returns>
        /// The default is <see cref="StoredMcpTask.HasExpired"/> (compares
        /// <see cref="StoredMcpTask.DiscardAfter"/>). Override for per-row Get and mutate.
        /// Set-based drains (EF <c>ExecuteDelete</c>) use the stamp; override
        /// <see cref="SweepExpiredCoreAsync"/> when bulk criteria must differ.
        /// </returns>
        protected virtual bool ShouldDiscard(StoredMcpTask task, DateTimeOffset now)
            => task.HasExpired(now);

        /// <summary>
        /// Pulses a throttled flush when <see cref="AutoSweep"/> is on.
        /// </summary>
        /// <param name="now">The current UTC time used for discard and throttle checks.</param>
        /// <param name="cancellationToken">Observed during the pass.</param>
        /// <returns>A completed task when auto-sweep is off or the interval has not elapsed.</returns>
        protected virtual Task PulseSweepAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (!AutoSweep || !TryClaimSweep(now))
            {
                return Task.CompletedTask;
            }

            return SweepExpiredCoreAsync(now, cancellationToken);
        }

        /// <summary>
        /// Claims the automatic-flush interval so only one pulse walks at a time.
        /// </summary>
        /// <param name="now">The current UTC time.</param>
        /// <returns>
        /// <see langword="true"/> when this caller should run the pass.
        /// A <see cref="SweepInterval"/> of <see cref="TimeSpan.Zero"/> always claims.
        /// </returns>
        protected virtual bool TryClaimSweep(DateTimeOffset now)
        {
            var intervalTicks = SweepInterval.Ticks;
            if (intervalTicks <= 0)
            {
                return true;
            }

            var last = Interlocked.Read(ref _lastSweepTicks);
            if (now.UtcTicks - last < intervalTicks)
            {
                return false;
            }

            return Interlocked.CompareExchange(ref _lastSweepTicks, now.UtcTicks, last) == last;
        }

        /// <summary>
        /// Returns the current user identifier, throwing when none is authenticated.
        /// </summary>
        /// <exception cref="McpTaskUserRequiredException">Thrown when the callback returns <see langword="null"/> or whitespace.</exception>
        protected virtual string RequireUserId()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                McpTaskUserRequiredException.Throw();
            }

            return userId!;
        }

        /// <summary>
        /// Returns the user identifier from <paramref name="user"/>, throwing when the principal is missing or unauthenticated.
        /// </summary>
        /// <param name="user">The principal for the current operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
        /// <exception cref="McpTaskUserRequiredException">Thrown when <paramref name="user"/> is unauthenticated.</exception>
        protected virtual string RequireUserId(ClaimsPrincipal user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var userId = GetUserId(user);
            if (string.IsNullOrWhiteSpace(userId))
            {
                McpTaskUserRequiredException.Throw();
            }

            return userId!;
        }

        /// <summary>
        /// Resolves a stable user identifier from <paramref name="principal"/>.
        /// </summary>
        /// <param name="principal">The principal for the current request.</param>
        /// <returns>
        /// The first non-whitespace claim in SDK session-identity order, or <see langword="null"/>
        /// when the principal is missing or unauthenticated.
        /// </returns>
        /// <remarks>
        /// Claim lookup matches the MCP C# SDK session identity order:
        /// <see cref="ClaimTypes.NameIdentifier"/>, <c>sub</c>, <see cref="ClaimTypes.Upn"/>,
        /// <see cref="ClaimTypes.Name"/>, then <see cref="IIdentity.Name"/>.
        /// The principal must be authenticated.
        /// </remarks>
        public static string? GetUserId(ClaimsPrincipal? principal)
        {
            var identity = principal?.Identity;
            if (identity is not { IsAuthenticated: true })
            {
                return null;
            }

            string? sub = null, upn = null, name = null;
            foreach (var claim in principal!.Claims)
            {
                var value = claim.Value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var type = claim.Type;
                if (type == ClaimTypes.NameIdentifier)
                {
                    return value;
                }

                if (sub is null && type == "sub")
                {
                    sub = value;
                }
                else if (upn is null && type == ClaimTypes.Upn)
                {
                    upn = value;
                }
                else if (name is null && type == ClaimTypes.Name)
                {
                    name = value;
                }
            }

            return sub ?? upn ?? name ?? identity.Name;
        }

        /// <summary>
        /// Returns whether <paramref name="ownerUserId"/> is the same user as <paramref name="currentUserId"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool IsOwnedBy(string? ownerUserId, string? currentUserId)
        {
            return !string.IsNullOrWhiteSpace(ownerUserId)
                && !string.IsNullOrWhiteSpace(currentUserId)
                && ownerUserId == currentUserId;
        }

        /// <summary>
        /// Raises <see cref="InputResponseReceived"/> for a resolved input.
        /// </summary>
        protected virtual void OnInputResponseReceived(string taskId, string requestId, InputResponse response)
        {
            var handler = InputResponseReceived;
            if (handler is null)
            {
                return;
            }

            handler(new InputResponseReceivedEventArgs
            {
                TaskId = taskId,
                RequestId = requestId,
                Response = response
            });
        }

        #endregion

    }

}
