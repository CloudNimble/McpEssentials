using System;
using System.Collections.Generic;
using System.Text.Json;
using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Redis.Tasks
{

    /// <summary>
    /// JSON shape of a Redis task key. Maps to <see cref="StoredMcpTask"/> at the store boundary.
    /// </summary>
    public class RedisStoredMcpTask
    {

        #region Properties

        /// <summary>
        /// Gets or sets the opaque task identifier.
        /// </summary>
        public string TaskId { get; set; } = "";

        /// <summary>
        /// Gets or sets the user recorded when the task was created.
        /// </summary>
        public string OwnerUserId { get; set; } = "";

        /// <summary>
        /// Gets or sets the protocol status (enum name).
        /// </summary>
        public McpTaskStatus Status { get; set; } = McpTaskStatus.Working;

        /// <summary>
        /// Gets or sets when the task was created, UTC.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the task last changed, UTC.
        /// </summary>
        public DateTimeOffset LastUpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time-to-live from create, or <see langword="null"/> for unlimited.
        /// </summary>
        public TimeSpan? TimeToLive { get; set; }

        /// <summary>
        /// Gets or sets the work-window end.
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets when the key should be discarded.
        /// </summary>
        public DateTimeOffset? DiscardAfter { get; set; }

        /// <summary>
        /// Gets or sets the client poll interval in milliseconds.
        /// </summary>
        public long? PollIntervalMs { get; set; }

        /// <summary>
        /// Gets or sets an optional status message.
        /// </summary>
        public string? StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the terminal result payload.
        /// </summary>
        public JsonElement? Result { get; set; }

        /// <summary>
        /// Gets or sets the JSON-RPC error payload.
        /// </summary>
        public JsonElement? Error { get; set; }

        /// <summary>
        /// Gets or sets outstanding input requests.
        /// </summary>
        public Dictionary<string, InputRequest>? InputRequests { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Copies <paramref name="stored"/> into a Redis JSON record.
        /// </summary>
        /// <param name="stored">The in-memory snapshot.</param>
        /// <returns>A JSON record with the same fields.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stored"/> is <see langword="null"/>.</exception>
        public static RedisStoredMcpTask From(StoredMcpTask stored)
        {
            ArgumentNullException.ThrowIfNull(stored);
            Dictionary<string, InputRequest>? inputs = null;
            if (stored.InputRequests is { Count: > 0 })
            {
                inputs = new Dictionary<string, InputRequest>(stored.InputRequests, StringComparer.Ordinal);
            }

            return new RedisStoredMcpTask
            {
                TaskId = stored.TaskId,
                OwnerUserId = stored.OwnerUserId,
                Status = stored.Status,
                CreatedAt = stored.CreatedAt,
                LastUpdatedAt = stored.LastUpdatedAt,
                TimeToLive = stored.TimeToLive,
                ExpiresAt = stored.ExpiresAt,
                DiscardAfter = stored.DiscardAfter,
                PollIntervalMs = stored.PollIntervalMs,
                StatusMessage = stored.StatusMessage,
                Result = stored.Result,
                Error = stored.Error,
                InputRequests = inputs
            };
        }

        /// <summary>
        /// Builds a <see cref="StoredMcpTask"/> from this record.
        /// </summary>
        /// <returns>A snapshot the Core <c>Try*</c> methods can mutate.</returns>
        public virtual StoredMcpTask ToStored()
        {
            return new StoredMcpTask
            {
                TaskId = TaskId,
                OwnerUserId = OwnerUserId,
                Status = Status,
                CreatedAt = CreatedAt,
                LastUpdatedAt = LastUpdatedAt,
                TimeToLive = TimeToLive,
                ExpiresAt = ExpiresAt,
                DiscardAfter = DiscardAfter,
                PollIntervalMs = PollIntervalMs,
                StatusMessage = StatusMessage,
                Result = Result,
                Error = Error,
                InputRequests = InputRequests is { Count: > 0 }
                    ? InputRequests
                    : StoredMcpTask.EmptyInputRequests
            };
        }

        #endregion

    }

}
