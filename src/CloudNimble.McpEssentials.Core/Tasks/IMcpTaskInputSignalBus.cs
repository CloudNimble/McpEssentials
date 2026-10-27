using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// Fan-out for <see cref="ModelContextProtocol.Extensions.Tasks.IMcpTaskStore.InputResponseReceived"/>
    /// across processes. Stores with no native pub/sub (Entity Framework Core, distributed cache,
    /// Azure Tables) take this as an optional constructor argument.
    /// </summary>
    /// <remarks>
    /// Payload is task id plus request id only. Do not put tokens, vault ids, or elicitation
    /// secrets on the bus; the store already has the response body. Missing bus plus multiple
    /// nodes means elicitation waits until timeout.
    /// </remarks>
    public interface IMcpTaskInputSignalBus
    {

        /// <summary>
        /// Publishes that <paramref name="requestId"/> on <paramref name="taskId"/> was resolved.
        /// </summary>
        /// <param name="taskId">The task whose input was resolved.</param>
        /// <param name="requestId">The input request identifier.</param>
        /// <param name="cancellationToken">Cancellation token for the publish.</param>
        Task PublishAsync(string taskId, string requestId, CancellationToken cancellationToken);

        /// <summary>
        /// Subscribes to resolved-input signals. The handler receives task id then request id.
        /// </summary>
        /// <param name="handler">Invoked when another node publishes a resolved input.</param>
        /// <returns>A subscription that stops delivery when disposed.</returns>
        IDisposable Subscribe(Action<string, string> handler);

    }

}
