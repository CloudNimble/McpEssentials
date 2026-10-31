using System;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using Microsoft.Azure.Cosmos;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// Exposes Cosmos store internals for coverage of protected members.
    /// </summary>
    internal class InspectableCosmosStore : CosmosMcpTaskStore
    {

        #region Constructors

        public InspectableCosmosStore(Func<CosmosClient> getClient, Func<string?> getCurrentUserId, ICosmosTaskGateway gateway, Func<IMcpTaskInputSignalBus?>? getBus = null)
            : base(getClient, getCurrentUserId, getBus, gateway)
        {
        }

        #endregion

        #region Public Methods

        public CosmosClient PublicClient => Client;

        public IMcpTaskInputSignalBus? PublicBus => SignalBus;

        public string PublicNewTaskId() => NewTaskId();

        #endregion

    }

}
