using CloudNimble.McpEssentials.Core.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Treats every caller as the owner so isolation overrides can be tested.
    /// </summary>
    internal class OpenIsolationStore : InMemoryMcpTaskStorePlus
    {

        #region Constructors

        public OpenIsolationStore()
            : base("alice")
        {
        }

        #endregion

        #region Protected Methods

        protected override bool IsOwnedBy(string? ownerUserId, string? currentUserId) => true;

        #endregion

    }

}
