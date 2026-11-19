using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Cosmos.Tasks;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// In-process <see cref="ICosmosTaskGateway"/> so store tests do not need an account.
    /// </summary>
    internal class InMemoryCosmosTaskGateway : ICosmosTaskGateway
    {

        #region Fields

        private readonly ConcurrentDictionary<string, (CosmosStoredMcpTask Document, string ETag)> _tasks = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, CosmosTaskIndex> _index = new(StringComparer.Ordinal);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the next <see cref="CreateIndexAsync"/> throws (to exercise rollback).
        /// </summary>
        public bool FailNextIndex { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public Task EnsureAsync(
            string databaseId,
            string tasksContainerId,
            string indexContainerId,
            int? throughput,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> TryCreateTaskAsync(CosmosStoredMcpTask document, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_tasks.TryAdd(document.Id, (Clone(document), "1")));
        }

        /// <inheritdoc />
        public Task CreateIndexAsync(CosmosTaskIndex index, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailNextIndex)
            {
                FailNextIndex = false;
                throw new InvalidOperationException("index write failed");
            }

            _index[index.Id] = index;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<CosmosTaskRead?> ReadTaskAsync(string taskId, string ownerUserId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_tasks.TryGetValue(taskId, out var entry) || !string.Equals(entry.Document.OwnerUserId, ownerUserId, StringComparison.Ordinal))
            {
                return Task.FromResult<CosmosTaskRead?>(null);
            }

            return Task.FromResult<CosmosTaskRead?>(new CosmosTaskRead(Clone(entry.Document), entry.ETag));
        }

        /// <inheritdoc />
        public Task<string?> ReadOwnerAsync(string taskId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_index.TryGetValue(taskId, out var index) ? index.OwnerUserId : null);
        }

        /// <inheritdoc />
        public Task<ApplyResult> TryReplaceTaskAsync(CosmosStoredMcpTask document, string etag, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_tasks.TryGetValue(document.Id, out var current))
            {
                return Task.FromResult(ApplyResult.Missing);
            }

            if (!string.Equals(current.ETag, etag, StringComparison.Ordinal))
            {
                return Task.FromResult(ApplyResult.Unchanged);
            }

            var next = int.Parse(current.ETag) + 1;
            _tasks[document.Id] = (Clone(document), next.ToString());
            return Task.FromResult(ApplyResult.Updated);
        }

        /// <inheritdoc />
        public Task DeleteTaskAsync(string taskId, string ownerUserId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _tasks.TryRemove(taskId, out _);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteIndexAsync(string taskId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _index.TryRemove(taskId, out _);
            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Copies <paramref name="document"/> so later mutations do not alias the store.
        /// </summary>
        /// <param name="document">The document to copy.</param>
        /// <returns>A shallow copy with cloned input-request map.</returns>
        private static CosmosStoredMcpTask Clone(CosmosStoredMcpTask document)
            => CosmosStoredMcpTask.From(document.ToStored(), document.LastUpdatedAt);

        #endregion

    }

}
