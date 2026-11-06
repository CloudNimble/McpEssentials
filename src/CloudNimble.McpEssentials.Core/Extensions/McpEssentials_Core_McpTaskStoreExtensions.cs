using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Extensions.Tasks
{

    /// <summary>
    /// Parks a fluent form on a task as a single elicitation <see cref="InputRequest"/>.
    /// Prefer <c>context.RequireForm</c> from a form-attributed tool. The SDK interceptor already
    /// calls <see cref="IMcpTaskStore.SetInputRequestsAsync(string, IDictionary{string, InputRequest}, CancellationToken)"/>
    /// when the tool is running as a task.
    /// </summary>
    public static class McpEssentials_Core_McpTaskStoreExtensions
    {

        #region Public Methods

        /// <summary>
        /// Parks <paramref name="form"/> on <paramref name="taskId"/> under a generated request id.
        /// </summary>
        /// <param name="store">The task store.</param>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="form">A form builder or field facade.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the form is stored.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="store"/> or <paramref name="form"/> is <see langword="null"/>.
        /// </exception>
        public static Task SetInputRequestsAsync(
            this IMcpTaskStore store,
            string taskId,
            IFormElicitationSource form,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(form);
            return store.SetInputRequestsAsync(taskId, form.ToInputRequest(), cancellationToken);
        }

        /// <summary>
        /// Parks <paramref name="form"/> on <paramref name="taskId"/> under <paramref name="requestId"/>.
        /// </summary>
        /// <param name="store">The task store.</param>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="requestId">The key used to match the later input response.</param>
        /// <param name="form">A form builder or field facade.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the form is stored.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="store"/> or <paramref name="form"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="requestId"/> is empty.</exception>
        public static Task SetInputRequestsAsync(
            this IMcpTaskStore store,
            string taskId,
            string requestId,
            IFormElicitationSource form,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(form);
            return store.SetInputRequestsAsync(taskId, requestId, form.ToInputRequest(), cancellationToken);
        }

        /// <summary>
        /// Parks a single <paramref name="request"/> on <paramref name="taskId"/> under a generated request id.
        /// </summary>
        /// <param name="store">The task store.</param>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="request">The input request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the request is stored.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="store"/> or <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        public static Task SetInputRequestsAsync(
            this IMcpTaskStore store,
            string taskId,
            InputRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(request);
            return store.SetInputRequestsAsync(
                taskId,
                new Dictionary<string, InputRequest>(1, StringComparer.Ordinal)
                {
                    [Guid.NewGuid().ToString("N")] = request
                },
                cancellationToken);
        }

        /// <summary>
        /// Parks a single <paramref name="request"/> on <paramref name="taskId"/> under <paramref name="requestId"/>.
        /// </summary>
        /// <param name="store">The task store.</param>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="requestId">The key used to match the later input response.</param>
        /// <param name="request">The input request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the request is stored.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="store"/> or <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="requestId"/> is empty.</exception>
        public static Task SetInputRequestsAsync(
            this IMcpTaskStore store,
            string taskId,
            string requestId,
            InputRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
            ArgumentNullException.ThrowIfNull(request);
            return store.SetInputRequestsAsync(
                taskId,
                new Dictionary<string, InputRequest>(1, StringComparer.Ordinal)
                {
                    [requestId] = request
                },
                cancellationToken);
        }

        #endregion

    }

}
