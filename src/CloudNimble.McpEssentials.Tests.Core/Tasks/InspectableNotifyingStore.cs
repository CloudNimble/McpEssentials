using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// In-memory <see cref="NotifyingMcpTaskStoreBase"/> that records publish/subscribe for coverage.
    /// </summary>
    internal class InspectableNotifyingStore : NotifyingMcpTaskStoreBase
    {

        #region Fields

        private readonly ConcurrentDictionary<string, StoredMcpTask> _tasks = new(StringComparer.Ordinal);
        private Action<string>? _handler;

        #endregion

        #region Properties

        /// <summary>
        /// Gets published pairs in order.
        /// </summary>
        public List<(string TaskId, string RequestId)> Published { get; } = [];

        /// <summary>
        /// Gets whether <see cref="SubscribeToInputNotifications"/> ran.
        /// </summary>
        public bool Subscribed { get; private set; }

        /// <summary>
        /// Gets whether <see cref="UnsubscribeFromInputNotifications"/> ran.
        /// </summary>
        public bool Unsubscribed { get; private set; }

        /// <summary>
        /// Gets or sets whether unsubscribe throws, covering the dispose catch.
        /// </summary>
        public bool ThrowOnUnsubscribe { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectableNotifyingStore"/> class.
        /// </summary>
        /// <param name="userId">The current user identifier.</param>
        public InspectableNotifyingStore(string? userId)
            : base(userId)
        {
        }

        /// <summary>
        /// Initializes a new instance from a principal factory.
        /// </summary>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        public InspectableNotifyingStore(Func<System.Security.Claims.ClaimsPrincipal?> getPrincipal)
            : base(getPrincipal)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Delivers a remote payload as the native channel would.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="requestId">The request identifier.</param>
        public void DeliverRemote(string? taskId, string? requestId)
        {
            if (taskId is null || requestId is null)
            {
                OnRemoteInputNotification(null);
                return;
            }

            OnRemoteInputNotification(McpTaskInputSignal.Format(taskId, requestId));
        }

        /// <summary>
        /// Delivers a raw channel payload as the native channel would.
        /// </summary>
        /// <param name="payload">The raw payload.</param>
        public void DeliverPayload(string? payload) => OnRemoteInputNotification(payload);

        /// <summary>
        /// Publishes as a successful resolve would.
        /// </summary>
        public Task PublicNotifyAsync(string taskId, IDictionary<string, InputResponse> responses, CancellationToken cancellationToken = default)
        {
            EnsureSubscription();
            return NotifyResolvedInputAsync(taskId, responses, cancellationToken);
        }

        /// <summary>
        /// Subscribes once for tests.
        /// </summary>
        public void PublicEnsure() => EnsureSubscription();

        #endregion

        #region Core Methods

        /// <inheritdoc />
        protected override Task<McpTaskInfo> CreateTaskCoreAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureSubscription();
            var stored = new StoredMcpTask(userId, NewTaskId(), GetUtcNow(), DefaultTimeToLive, DefaultPollIntervalMs);
            _tasks[stored.TaskId] = stored;
            return Task.FromResult(stored.Info);
        }

        /// <inheritdoc />
        protected override Task<McpTaskInfo?> GetTaskCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_tasks.TryGetValue(taskId, out var stored) && IsOwnedBy(stored.OwnerUserId, userId) ? stored.Info : null);
        }

        /// <inheritdoc />
        protected override Task<bool> SetCancelledCoreAsync(string taskId, string userId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        /// <inheritdoc />
        protected override Task ResolveInputRequestsCoreAsync(string taskId, IDictionary<string, InputResponse> inputResponses, string userId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        protected override Task SetCompletedCoreAsync(string taskId, JsonElement result, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        protected override Task SetFailedCoreAsync(string taskId, JsonElement error, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        protected override Task SetInputRequestsCoreAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        protected override Task SweepExpiredCoreAsync(DateTimeOffset now, CancellationToken cancellationToken)
            => Task.CompletedTask;

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override Task PublishInputNotificationAsync(string payload, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (McpTaskInputSignal.TryParse(payload, out var taskId, out var requestId))
            {
                Published.Add((taskId, requestId));
            }

            _handler?.Invoke(payload);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override void SubscribeToInputNotifications()
        {
            Subscribed = true;
            _handler = OnRemoteInputNotification;
        }

        /// <inheritdoc />
        protected override void UnsubscribeFromInputNotifications()
        {
            Unsubscribed = true;
            if (ThrowOnUnsubscribe)
            {
                throw new ObjectDisposedException(nameof(InspectableNotifyingStore));
            }
        }

        #endregion

    }

}
