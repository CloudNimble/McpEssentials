using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Cosmos.Tasks
{

    /// <summary>
    /// Cosmos document for a task. <see cref="Id"/> is the Cosmos item id (the task id).
    /// Partition key is <see cref="OwnerUserId"/>.
    /// </summary>
    public class CosmosStoredMcpTask
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Cosmos item id (the opaque task identifier).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the user recorded when the task was created. Partition key.
        /// </summary>
        [JsonPropertyName("ownerUserId")]
        public string OwnerUserId { get; set; } = "";

        /// <summary>
        /// Gets or sets Cosmos per-document TTL in seconds, or <see langword="null"/> when unlimited.
        /// </summary>
        [JsonPropertyName("ttl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TimeToLiveSeconds { get; set; }

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
        /// Gets or sets when the document should be discarded.
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
        /// Copies <paramref name="stored"/> into a Cosmos document.
        /// </summary>
        /// <param name="stored">The in-memory snapshot.</param>
        /// <param name="now">The current UTC time, used to stamp <see cref="TimeToLiveSeconds"/>.</param>
        /// <returns>A document with the same fields.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stored"/> is <see langword="null"/>.</exception>
        public static CosmosStoredMcpTask From(StoredMcpTask stored, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(stored);
            return new CosmosStoredMcpTask
            {
                Id = stored.TaskId,
                OwnerUserId = stored.OwnerUserId,
                TimeToLiveSeconds = ToTtlSeconds(stored.DiscardAfter, now),
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
                InputRequests = stored.ToInputRequestDictionaryOrNull()
            };
        }

        /// <summary>
        /// Builds a <see cref="StoredMcpTask"/> from this document.
        /// </summary>
        /// <returns>A snapshot the Core <c>Try*</c> methods can mutate.</returns>
        public virtual StoredMcpTask ToStored()
        {
            return new StoredMcpTask
            {
                TaskId = Id,
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

        /// <summary>
        /// Converts <paramref name="discardAfter"/> into Cosmos TTL seconds from <paramref name="now"/>.
        /// </summary>
        /// <param name="discardAfter">The visibility ceiling, or <see langword="null"/> when unlimited.</param>
        /// <param name="now">The current UTC time.</param>
        /// <returns>At least 1 second, or <see langword="null"/> when unlimited.</returns>
        public static int? ToTtlSeconds(DateTimeOffset? discardAfter, DateTimeOffset now)
        {
            if (discardAfter is not { } deadline)
            {
                return null;
            }

            var seconds = (int)Math.Ceiling((deadline - now).TotalSeconds);
            return seconds < 1 ? 1 : seconds;
        }

        #endregion

    }

}
