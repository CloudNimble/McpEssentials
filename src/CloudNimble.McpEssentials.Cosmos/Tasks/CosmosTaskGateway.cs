using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;
using Microsoft.Azure.Cosmos;

namespace CloudNimble.McpEssentials.Cosmos.Tasks
{

    /// <summary>
    /// Cosmos SDK adapter. Network I/O lives here so <see cref="CosmosMcpTaskStore"/>
    /// can be tested against an in-memory gateway.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class CosmosTaskGateway : ICosmosTaskGateway
    {

        #region Fields

        private readonly Func<CosmosClient> _getClient;
        private string _databaseId = "mcpessentials";
        private string _tasksContainerId = "McpTasks";
        private string _indexContainerId = "McpTaskIndex";
        private Container? _tasks;
        private Container? _index;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance for <paramref name="getClient"/>.
        /// </summary>
        /// <param name="getClient">Returns the Cosmos client. The host owns its lifetime.</param>
        public CosmosTaskGateway(Func<CosmosClient> getClient)
        {
            _getClient = getClient;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public async Task EnsureAsync(
            string databaseId,
            string tasksContainerId,
            string indexContainerId,
            int? throughput,
            CancellationToken cancellationToken)
        {
            _databaseId = databaseId;
            _tasksContainerId = tasksContainerId;
            _indexContainerId = indexContainerId;
            _tasks = null;
            _index = null;
            var client = _getClient();
            DatabaseResponse databaseResponse = throughput is int ru
                ? await client.CreateDatabaseIfNotExistsAsync(databaseId, ru, cancellationToken: cancellationToken).ConfigureAwait(false)
                : await client.CreateDatabaseIfNotExistsAsync(databaseId, cancellationToken: cancellationToken).ConfigureAwait(false);
            var database = databaseResponse.Database;
            await CreateContainerIfNotExistsAsync(
                    database,
                    new ContainerProperties(tasksContainerId, "/ownerUserId") { DefaultTimeToLive = -1 },
                    throughput,
                    cancellationToken)
                .ConfigureAwait(false);
            await CreateContainerIfNotExistsAsync(
                    database,
                    new ContainerProperties(indexContainerId, "/id") { DefaultTimeToLive = -1 },
                    throughput,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> TryCreateTaskAsync(CosmosStoredMcpTask document, CancellationToken cancellationToken)
        {
            try
            {
                await Tasks().CreateItemAsync(document, new PartitionKey(document.OwnerUserId), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                return false;
            }
        }

        /// <inheritdoc />
        public Task CreateIndexAsync(CosmosTaskIndex index, CancellationToken cancellationToken)
            => Index().CreateItemAsync(index, new PartitionKey(index.Id), cancellationToken: cancellationToken);

        /// <inheritdoc />
        public async Task<CosmosTaskRead?> ReadTaskAsync(string taskId, string ownerUserId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await Tasks()
                    .ReadItemAsync<CosmosStoredMcpTask>(taskId, new PartitionKey(ownerUserId), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return new CosmosTaskRead(response.Resource, response.ETag);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string?> ReadOwnerAsync(string taskId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await Index()
                    .ReadItemAsync<CosmosTaskIndex>(taskId, new PartitionKey(taskId), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return response.Resource.OwnerUserId;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<ApplyResult> TryReplaceTaskAsync(CosmosStoredMcpTask document, string etag, CancellationToken cancellationToken)
        {
            try
            {
                await Tasks().ReplaceItemAsync(
                        document,
                        document.Id,
                        new PartitionKey(document.OwnerUserId),
                        new ItemRequestOptions { IfMatchEtag = etag },
                        cancellationToken)
                    .ConfigureAwait(false);
                return ApplyResult.Updated;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return ApplyResult.Unchanged;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return ApplyResult.Missing;
            }
        }

        /// <inheritdoc />
        public Task DeleteTaskAsync(string taskId, string ownerUserId, CancellationToken cancellationToken)
            => DeleteQuietAsync(Tasks(), taskId, new PartitionKey(ownerUserId), cancellationToken);

        /// <inheritdoc />
        public Task DeleteIndexAsync(string taskId, CancellationToken cancellationToken)
            => DeleteQuietAsync(Index(), taskId, new PartitionKey(taskId), cancellationToken);

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a container, using <paramref name="throughput"/> when set.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="properties">Container id, partition key, and TTL defaults.</param>
        /// <param name="throughput">RU/s, or <see langword="null"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private static async Task CreateContainerIfNotExistsAsync(
            Database database,
            ContainerProperties properties,
            int? throughput,
            CancellationToken cancellationToken)
        {
            if (throughput is int ru)
            {
                await database.CreateContainerIfNotExistsAsync(properties, ru, cancellationToken: cancellationToken).ConfigureAwait(false);
                return;
            }

            await database.CreateContainerIfNotExistsAsync(properties, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an item, ignoring not-found.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="id">The item id.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private static async Task DeleteQuietAsync(Container container, string id, PartitionKey partitionKey, CancellationToken cancellationToken)
        {
            try
            {
                await container.DeleteItemAsync<CosmosStoredMcpTask>(id, partitionKey, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
            }
        }

        /// <summary>
        /// Returns the tasks container from the current client.
        /// </summary>
        /// <returns>The tasks container.</returns>
        private Container Tasks()
            => _tasks ??= _getClient().GetContainer(_databaseId, _tasksContainerId);

        /// <summary>
        /// Returns the index container from the current client.
        /// </summary>
        /// <returns>The index container.</returns>
        private Container Index()
            => _index ??= _getClient().GetContainer(_databaseId, _indexContainerId);

        #endregion

    }

}
