using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// In-process <see cref="IMcpTaskInputSignalBus"/> that records publishes and does not echo
    /// them to subscribers (the publishing node already raised the local event).
    /// </summary>
    internal class RecordingSignalBus : IMcpTaskInputSignalBus
    {

        #region Fields

        private Action<string, string>? _handler;

        #endregion

        #region Properties

        public List<(string TaskId, string RequestId)> Published { get; } = [];

        #endregion

        #region Public Methods

        public Task PublishAsync(string taskId, string requestId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Published.Add((taskId, requestId));
            return Task.CompletedTask;
        }

        public IDisposable Subscribe(Action<string, string> handler)
        {
            _handler += handler;
            return new SignalBusSubscription(() => _handler -= handler);
        }

        public void Deliver(string taskId, string requestId) => _handler?.Invoke(taskId, requestId);

        #endregion

    }

}
