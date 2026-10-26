using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// An <see cref="IMcpTaskStore"/> that isolates tasks per authenticated user and exposes
    /// <see cref="ClaimsPrincipal"/> overloads for every client-facing operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The MCP C# SDK calls the <see cref="IMcpTaskStore"/> methods, which have no user parameter.
    /// Authenticated stores resolve the caller from the current-user callback captured at construction.
    /// Hosts, tests, and durable providers that already have a <see cref="ClaimsPrincipal"/>
    /// call the overloads on this interface instead.
    /// </para>
    /// <para>
    /// Client-facing operations (create, get, cancel, resolve) require an authenticated principal
    /// and treat another user's task as missing. Server-side completion (set completed, set failed,
    /// set input requests) stays on <see cref="IMcpTaskStore"/> and skips the live-user check because
    /// the SDK runs those from a background DI scope after the originating request is gone.
    /// </para>
    /// </remarks>
    public interface IPerUserMcpTaskStore : IMcpTaskStore
    {

        /// <summary>
        /// Creates a new task owned by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The authenticated principal that will own the task.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>
        /// A <see cref="McpTaskInfo"/> with a unique task id, initial status of
        /// <see cref="McpTaskStatus.Working"/>, and timing metadata.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
        /// <exception cref="McpTaskUserRequiredException">Thrown when <paramref name="user"/> is unauthenticated.</exception>
        Task<McpTaskInfo> CreateTaskAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the current state of a task when <paramref name="user"/> owns it.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task to retrieve.</param>
        /// <param name="user">The authenticated principal making the request.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>
        /// The current task state, or <see langword="null"/> when the task is missing, expired, or owned by another user.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="taskId"/> is <see langword="null"/> or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
        /// <exception cref="McpTaskUserRequiredException">Thrown when <paramref name="user"/> is unauthenticated.</exception>
        Task<McpTaskInfo?> GetTaskAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions the task to <see cref="McpTaskStatus.Cancelled"/> when <paramref name="user"/> owns it.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task to cancel.</param>
        /// <param name="user">The authenticated principal making the request.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>
        /// <see langword="true"/> when the task was cancelled;
        /// <see langword="false"/> when the task is missing, expired, already terminal, or owned by another user.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="taskId"/> is <see langword="null"/> or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
        /// <exception cref="McpTaskUserRequiredException">Thrown when <paramref name="user"/> is unauthenticated.</exception>
        Task<bool> SetCancelledAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes matched input requests and raises <see cref="IMcpTaskStore.InputResponseReceived"/>
        /// when <paramref name="user"/> owns the task.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="inputResponses">The input responses keyed by the original request identifier.</param>
        /// <param name="user">The authenticated principal making the request.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="taskId"/> is <see langword="null"/> or whitespace.</exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="inputResponses"/> or <paramref name="user"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="McpTaskUserRequiredException">Thrown when <paramref name="user"/> is unauthenticated.</exception>
        Task ResolveInputRequestsAsync(string taskId, IDictionary<string, InputResponse> inputResponses, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    }

}
