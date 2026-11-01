using System;
using System.Threading;

namespace CloudNimble.McpEssentials.PostgreSQL.Tasks
{

    /// <summary>
    /// Stops a <c>LISTEN</c> loop when disposed.
    /// </summary>
    internal class PostgreSqlNotificationSubscription : IDisposable
    {

        #region Fields

        private readonly CancellationTokenSource _cancellation = new();
        private readonly Action _onDispose;
        private int _disposed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the token that cancels the listen loop.
        /// </summary>
        public CancellationToken Token => _cancellation.Token;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlNotificationSubscription"/> class.
        /// </summary>
        /// <param name="onDispose">Runs after the token is cancelled (close the listen connection).</param>
        public PostgreSqlNotificationSubscription(Action onDispose)
        {
            _onDispose = onDispose;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            _cancellation.Cancel();
            _onDispose();
            _cancellation.Dispose();
        }

        #endregion

    }

}
