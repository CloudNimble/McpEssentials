using System;
using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;

namespace CloudNimble.McpEssentials.PostgreSQL.Tasks
{

    /// <summary>
    /// One <c>McpTasks</c> row. Maps to <see cref="StoredMcpTask"/> at the store boundary
    /// and adds <see cref="RowVersion"/> for compare-and-swap.
    /// </summary>
    public class PostgreSqlStoredMcpTask
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
        /// Gets or sets when the row should be discarded.
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
        /// Gets or sets the terminal result as JSON.
        /// </summary>
        public string? ResultJson { get; set; }

        /// <summary>
        /// Gets or sets the JSON-RPC error as JSON.
        /// </summary>
        public string? ErrorJson { get; set; }

        /// <summary>
        /// Gets or sets the outstanding input-request map as JSON.
        /// </summary>
        public string? InputRequestsJson { get; set; }

        /// <summary>
        /// Gets or sets the compare-and-swap token. Incremented on every write.
        /// </summary>
        public long RowVersion { get; set; } = 1;

        #endregion

        #region Public Methods

        /// <summary>
        /// Copies <paramref name="stored"/> into a row. <see cref="RowVersion"/> stays at 1
        /// for inserts; callers bump it on update.
        /// </summary>
        /// <param name="stored">The in-memory snapshot.</param>
        /// <returns>A row with the same engine fields.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stored"/> is <see langword="null"/>.</exception>
        public static PostgreSqlStoredMcpTask From(StoredMcpTask stored)
        {
            ArgumentNullException.ThrowIfNull(stored);
            return new PostgreSqlStoredMcpTask
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
                ResultJson = StoredMcpTask.ToJson(stored.Result),
                ErrorJson = StoredMcpTask.ToJson(stored.Error),
                InputRequestsJson = StoredMcpTask.ToInputRequestsJson(stored.InputRequests)
            };
        }

        /// <summary>
        /// Builds a <see cref="StoredMcpTask"/> from this row.
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
                Result = StoredMcpTask.ParseElement(ResultJson),
                Error = StoredMcpTask.ParseElement(ErrorJson),
                InputRequests = StoredMcpTask.ParseInputRequests(InputRequestsJson)
            };
        }

        #endregion

    }

}
