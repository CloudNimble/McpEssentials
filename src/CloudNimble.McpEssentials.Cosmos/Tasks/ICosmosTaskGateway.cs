using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;

namespace CloudNimble.McpEssentials.Cosmos.Tasks
{

    /// <summary>
    /// Document I/O for <see cref="CosmosMcpTaskStore"/>. Production talks to Cosmos;
    /// tests supply an in-memory implementation so store logic runs without an account.
    /// </summary>
    internal interface ICosmosTaskGateway
    {

        /// <summary>
        /// Creates the database and containers when they are missing.
        /// </summary>
        /// <param name="databaseId">The database id.</param>
        /// <param name="tasksContainerId">The tasks container id.</param>
        /// <param name="indexContainerId">The index container id.</param>
        /// <param name="throughput">RU/s, or <see langword="null"/> for serverless.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task EnsureAsync(
            string databaseId,
            string tasksContainerId,
            string indexContainerId,
            int? throughput,
            CancellationToken cancellationToken);

        /// <summary>
        /// Creates the task document. Returns <see langword="false"/> on id conflict.
        /// </summary>
        /// <param name="document">The task document.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see langword="true"/> when the document was created.</returns>
        Task<bool> TryCreateTaskAsync(CosmosStoredMcpTask document, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the index row.
        /// </summary>
        /// <param name="index">The index document.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CreateIndexAsync(CosmosTaskIndex index, CancellationToken cancellationToken);

        /// <summary>
        /// Point-reads a task document.
        /// </summary>
        /// <param name="taskId">The item id.</param>
        /// <param name="ownerUserId">The partition key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The document and ETag, or <see langword="null"/> when missing.</returns>
        Task<CosmosTaskRead?> ReadTaskAsync(string taskId, string ownerUserId, CancellationToken cancellationToken);

        /// <summary>
        /// Reads the owner from the index container.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The owner user id, or <see langword="null"/> when missing.</returns>
        Task<string?> ReadOwnerAsync(string taskId, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces the task document when the ETag matches.
        /// </summary>
        /// <param name="document">The next snapshot.</param>
        /// <param name="etag">The expected ETag.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// <see cref="ApplyResult.Updated"/> when replaced,
        /// <see cref="ApplyResult.Missing"/> when the item is gone,
        /// <see cref="ApplyResult.Unchanged"/> on ETag conflict (retry).
        /// </returns>
        Task<ApplyResult> TryReplaceTaskAsync(CosmosStoredMcpTask document, string etag, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the task document, ignoring not-found.
        /// </summary>
        /// <param name="taskId">The item id.</param>
        /// <param name="ownerUserId">The partition key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteTaskAsync(string taskId, string ownerUserId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the index row, ignoring not-found.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteIndexAsync(string taskId, CancellationToken cancellationToken);

    }

}
