using System;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// Disposes a <see cref="RecordingSignalBus"/> subscription.
    /// </summary>
    internal class RecordingSignalBusSubscription : IDisposable
    {

        #region Fields

        private readonly Action _onDispose;

        #endregion

        #region Constructors

        public RecordingSignalBusSubscription(Action onDispose)
        {
            _onDispose = onDispose;
        }

        #endregion

        #region Public Methods

        public void Dispose() => _onDispose();

        #endregion

    }

}
