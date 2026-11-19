using System;
using System.Threading;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// Removes an in-memory notification handler once.
    /// </summary>
    internal sealed class InMemoryPostgreSqlSubscription : IDisposable
    {

        #region Fields

        private readonly Action _onDispose;
        private int _disposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryPostgreSqlSubscription"/> class.
        /// </summary>
        /// <param name="onDispose">Runs the first time <see cref="Dispose"/> is called.</param>
        public InMemoryPostgreSqlSubscription(Action onDispose)
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

            _onDispose();
        }

        #endregion

    }

}
