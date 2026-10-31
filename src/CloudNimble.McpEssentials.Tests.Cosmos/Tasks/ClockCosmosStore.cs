using System;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using Microsoft.Azure.Cosmos;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// Cosmos store with a controllable clock for DiscardAfter tests.
    /// </summary>
    internal class ClockCosmosStore : CosmosMcpTaskStore
    {

        #region Properties

        public DateTimeOffset Now { get; set; }

        #endregion

        #region Constructors

        public ClockCosmosStore(Func<CosmosClient> getClient, string? userId, DateTimeOffset now, ICosmosTaskGateway gateway)
            : base(getClient, () => userId, null, gateway)
        {
            Now = now;
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override DateTimeOffset GetUtcNow() => Now;

        #endregion

    }

}
