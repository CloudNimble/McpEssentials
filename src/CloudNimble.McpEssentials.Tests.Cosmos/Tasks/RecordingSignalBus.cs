using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// In-process bus that records subscribe/dispose for Cosmos store coverage.
    /// </summary>
    internal class RecordingSignalBus : IMcpTaskInputSignalBus
    {

        #region Properties

        public int SubscribeCount { get; private set; }

        public int DisposeCount { get; private set; }

        #endregion

        #region Public Methods

        public Task PublishAsync(string taskId, string requestId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public IDisposable Subscribe(Action<string, string> handler)
        {
            SubscribeCount++;
            handler("t", "r");
            return new RecordingSignalBusSubscription(() => DisposeCount++);
        }

        #endregion

    }

}
