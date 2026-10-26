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
    /// identity, <see cref="InputResponseReceived"/>, and the mapping from
    /// <see cref="IMcpTaskStore"/> / <see cref="IPerUserMcpTaskStore"/> into per-user core methods.
    /// </summary>
    /// <remarks>
    /// Subclass and implement the <c>Core</c> methods to persist snapshots. Do not wrap the SDK
    /// <c>InMemoryMcpTaskStore</c>; each provider owns its storage.
    /// </remarks>
    public abstract class AuthenticatedMcpTaskStoreBase : IPerUserMcpTaskStore
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

        #endregion

        #region Properties

        /// <summary>
        /// Gets the callback used by the <see cref="IMcpTaskStore"/> methods to resolve the current user.
        /// </summary>
        protected Func<string?> GetCurrentUserId { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedMcpTaskStoreBase"/> class.
        /// </summary>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        protected AuthenticatedMcpTaskStoreBase(Func<string?> getCurrentUserId)
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            GetCurrentUserId = getCurrentUserId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedMcpTaskStoreBase"/> class
        /// that always uses <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user identifier, or <see langword="null"/> to treat the process as unauthenticated.</param>
        protected AuthenticatedMcpTaskStoreBase(string? userId)
            : this(() => userId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedMcpTaskStoreBase"/> class
        /// that resolves the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        protected AuthenticatedMcpTaskStoreBase(Func<ClaimsPrincipal?> getPrincipal)
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
        public virtual Task<McpTaskInfo> CreateTaskAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return CreateTaskCoreAsync(RequireUserId(), cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<McpTaskInfo?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            return GetTaskCoreAsync(taskId, RequireUserId(), cancellationToken);
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

        #region IPerUserMcpTaskStore

        /// <inheritdoc />
        public virtual Task<McpTaskInfo> CreateTaskAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return CreateTaskCoreAsync(RequireUserId(user), cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<McpTaskInfo?> GetTaskAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            return GetTaskCoreAsync(taskId, RequireUserId(user), cancellationToken);
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
        /// Returns whether <paramref name="status"/> is a terminal task status.
        /// </summary>
        protected virtual bool IsTerminal(McpTaskStatus status)
            => status is McpTaskStatus.Completed or McpTaskStatus.Cancelled or McpTaskStatus.Failed;

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
