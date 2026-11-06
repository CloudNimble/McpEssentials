using System.Collections.Generic;
using ModelContextProtocol.Server;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Test double for <see cref="IMcpServerPrimitive"/> metadata inspection.
    /// </summary>
    public class FakeMcpServerPrimitive : IMcpServerPrimitive
    {

        #region Properties

        /// <inheritdoc />
        public string Id { get; set; } = "tool";

        /// <inheritdoc />
        public IReadOnlyList<object> Metadata { get; set; } = [];

        #endregion

    }

}
