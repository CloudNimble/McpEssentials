using System;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// Removes a <see cref="RecordingSignalBus"/> handler when disposed.
    /// </summary>
    internal class SignalBusSubscription : IDisposable
    {

        #region Fields

        private readonly Action _unsubscribe;

        #endregion

        #region Constructors

        public SignalBusSubscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        #endregion

        #region Public Methods

        public void Dispose() => _unsubscribe();

        #endregion

    }

}
