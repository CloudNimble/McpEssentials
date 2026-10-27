using CloudNimble.McpEssentials.Core.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Loses the first compare-and-swap so <see cref="InMemoryAuthenticatedMcpTaskStore.TryApply"/> retries.
    /// </summary>
    internal class ContendedStore : InMemoryAuthenticatedMcpTaskStore
    {

        #region Properties

        public int FailedSwaps { get; private set; }

        #endregion

        #region Constructors

        public ContendedStore()
            : base("alice")
        {
        }

        #endregion

        #region Protected Methods

        protected override bool TryCompareAndSwap(string taskId, StoredMcpTask current, StoredMcpTask next)
        {
            if (FailedSwaps == 0)
            {
                FailedSwaps++;
                return false;
            }

            return base.TryCompareAndSwap(taskId, current, next);
        }

        #endregion

    }

}
