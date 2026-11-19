using CloudNimble.McpEssentials.Core.Tasks;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// In-memory store whose flush treats every row as expired.
    /// </summary>
    internal class AlwaysDiscardStore : InspectableStore
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance owned by <c>alice</c>.
        /// </summary>
        public AlwaysDiscardStore()
            : base("alice")
        {
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override bool ShouldDiscard(StoredMcpTask task, DateTimeOffset now) => true;

        #endregion

    }

}
