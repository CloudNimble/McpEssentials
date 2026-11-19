using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// <see cref="McpTaskStoreBase"/> for engines that fan resolved input over a native
    /// notification channel (Redis pub/sub, Postgres <c>NOTIFY</c>, NATS). Subclasses
    /// implement publish, subscribe, and unsubscribe. This type skips this process's
    /// own echo and raises <see cref="McpTaskStoreBase.InputResponseReceived"/> for remote payloads.
    /// </summary>
    /// <remarks>
    /// Call <see cref="EnsureSubscription"/> from request-path core methods. After a successful
    /// local resolve, call <see cref="NotifyResolvedInputAsync"/>. Providers without a native
    /// channel (EF Core, Cosmos v1) keep using optional <see cref="IMcpTaskInputSignalBus"/>.
    /// </remarks>
    public abstract class NotifyingMcpTaskStoreBase : McpTaskStoreBase, IDisposable
    {

        #region Fields

        private readonly ConcurrentDictionary<string, byte> _localPublishes = new(StringComparer.Ordinal);
        private int _subscribed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyingMcpTaskStoreBase"/> class.
        /// </summary>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        protected NotifyingMcpTaskStoreBase(Func<string?> getCurrentUserId)
            : base(getCurrentUserId)
        {
        }

        /// <summary>
        /// Initializes a new instance that always uses <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user identifier, or <see langword="null"/> to treat the process as unauthenticated.</param>
        protected NotifyingMcpTaskStoreBase(string? userId)
            : base(userId)
        {
        }

        /// <summary>
        /// Initializes a new instance that resolves the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        protected NotifyingMcpTaskStoreBase(Func<ClaimsPrincipal?> getPrincipal)
            : base(getPrincipal)
        {
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public virtual void Dispose()
        {
            try
            {
                UnsubscribeFromInputNotifications();
            }
            catch (Exception)
            {
            }

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Publishes a resolved-input payload on the engine's native channel.
        /// </summary>
        /// <param name="payload">
        /// <see cref="McpTaskInputSignal.Format"/> of the task id and request id.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the engine has accepted the publish.</returns>
        protected abstract Task PublishInputNotificationAsync(string payload, CancellationToken cancellationToken);

        /// <summary>
        /// Starts listening for remote resolved-input signals. Invoke
        /// <see cref="OnRemoteInputNotification"/> for each payload. Called once.
        /// </summary>
        protected abstract void SubscribeToInputNotifications();

        /// <summary>
        /// Stops listening for remote resolved-input signals.
        /// </summary>
        protected abstract void UnsubscribeFromInputNotifications();

        /// <summary>
        /// Subscribes to the native channel once so other nodes can wake this process.
        /// </summary>
        protected void EnsureSubscription()
        {
            if (Interlocked.Exchange(ref _subscribed, 1) == 1)
            {
                return;
            }

            SubscribeToInputNotifications();
        }

        /// <summary>
        /// Records a local publish token, then publishes on the native channel.
        /// Remote handlers skip this process's own echo.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="requestId">The input request identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the engine has accepted the publish.</returns>
        protected Task PublishInputAsync(string taskId, string requestId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = McpTaskInputSignal.Format(taskId, requestId);
            _localPublishes.TryAdd(payload, 0);
            return PublishInputNotificationAsync(payload, cancellationToken);
        }

        /// <summary>
        /// Raises the local event for each resolved input, then publishes so other nodes wake.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="inputResponses">The responses just persisted.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when every publish has been accepted.</returns>
        protected async Task NotifyResolvedInputAsync(
            string taskId,
            IDictionary<string, InputResponse> inputResponses,
            CancellationToken cancellationToken)
        {
            foreach (var pair in inputResponses)
            {
                OnInputResponseReceived(taskId, pair.Key, pair.Value);
                await PublishInputAsync(taskId, pair.Key, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handles a payload from the native channel. Empty or malformed payloads are ignored.
        /// Echoes of this process's own publish are dropped without parsing. Remote payloads
        /// raise a stub <see cref="McpTaskStoreBase.EmptyInputResponse"/>.
        /// </summary>
        /// <param name="payload">The raw channel payload, or <see langword="null"/> when missing.</param>
        protected void OnRemoteInputNotification(string? payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return;
            }

            if (_localPublishes.TryRemove(payload, out _))
            {
                return;
            }

            if (!McpTaskInputSignal.TryParse(payload, out var taskId, out var requestId))
            {
                return;
            }

            OnInputResponseReceived(taskId, requestId, EmptyInputResponse);
        }

        #endregion

    }

}
