using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Counts <see cref="InMemoryMcpTaskStorePlus.CreateTaskAsync"/> calls.
    /// </summary>
    internal class CountingStore : InMemoryMcpTaskStorePlus
    {

        #region Properties

        public int Creates { get; private set; }

        #endregion

        #region Constructors

        public CountingStore()
            : base("alice")
        {
        }

        #endregion

        #region Public Methods

        public override Task<McpTaskInfo> CreateTaskAsync(CancellationToken cancellationToken = default)
        {
            Creates++;
            return base.CreateTaskAsync(cancellationToken);
        }

        #endregion

    }

}
