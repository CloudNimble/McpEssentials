using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// Persistence foundation for an MCP task. Protocol fields plus owner and expiry.
    /// Construct from <see cref="McpTaskInfo"/>, mutate with <c>Try*</c>, and emit the SDK record
    /// with <see cref="ToMcpTaskInfo"/>. Durable providers subclass this type (for example
    /// <c>McpTaskEntity</c>) and add only engine-specific columns.
    /// </summary>
    public class StoredMcpTask
    {

        #region Fields

        /// <summary>
        /// Shared empty input-request map. Stable instance so reads do not allocate.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, InputRequest> EmptyInputRequests =
            new Dictionary<string, InputRequest>(0, StringComparer.Ordinal);

        private McpTaskInfo? _info;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the opaque task identifier. Primary key. Do not embed the user id.
        /// </summary>
        public string TaskId { get; set; } = "";

        /// <summary>
        /// Gets or sets the user recorded when the task was created. Isolation key.
        /// </summary>
        public string OwnerUserId { get; set; } = "";

        /// <summary>
        /// Gets or sets the protocol status.
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
        /// Gets or sets the time-to-live from <see cref="CreatedAt"/>, or <see langword="null"/> for unlimited.
        /// </summary>
        public TimeSpan? TimeToLive { get; set; }

        /// <summary>
        /// Gets or sets the work-window end (<c>CreatedAt + TimeToLive</c>).
        /// <see langword="null"/> when unlimited. Discard uses <see cref="DiscardAfter"/>.
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets when the row should be discarded. Stamped at write time as
        /// <c>CreatedAt + 2×TimeToLive</c>. <see langword="null"/> when unlimited.
        /// </summary>
        /// <remarks>
        /// Two windows is the visibility ceiling, including late complete, fail, or cancel.
        /// The stamp does not move when the task finishes. <see cref="HasExpired"/> compares
        /// the current time to this value.
        /// </remarks>
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
        /// Gets or sets the terminal <c>CallToolResult</c>, or <see langword="null"/>.
        /// </summary>
        public JsonElement? Result { get; set; }

        /// <summary>
        /// Gets or sets the JSON-RPC error, or <see langword="null"/>.
        /// </summary>
        public JsonElement? Error { get; set; }

        /// <summary>
        /// Gets or sets the outstanding input-request map. Empty when none are pending.
        /// </summary>
        public IReadOnlyDictionary<string, InputRequest> InputRequests { get; set; } = EmptyInputRequests;

        /// <summary>
        /// Gets the cached protocol snapshot. Allocates once per mutation.
        /// </summary>
        public McpTaskInfo Info => ToMcpTaskInfo();

        /// <summary>
        /// Gets whether <see cref="Status"/> is completed, cancelled, or failed.
        /// </summary>
        public virtual bool IsFinished =>
            Status is McpTaskStatus.Completed or McpTaskStatus.Cancelled or McpTaskStatus.Failed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty instance. Used by materializers (EF Core). Prefer
        /// <see cref="StoredMcpTask(string, McpTaskInfo)"/> when the owner and snapshot are known.
        /// </summary>
        public StoredMcpTask()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StoredMcpTask"/> class from a protocol snapshot.
        /// </summary>
        /// <param name="ownerUserId">The user recorded when the task was created.</param>
        /// <param name="info">The protocol-facing snapshot. Must not include the owner.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="ownerUserId"/> is <see langword="null"/> or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is <see langword="null"/>.</exception>
        public StoredMcpTask(string ownerUserId, McpTaskInfo info)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
            ArgumentNullException.ThrowIfNull(info);

            OwnerUserId = ownerUserId;
            Apply(info);
        }

        /// <summary>
        /// Initializes a new working task owned by <paramref name="ownerUserId"/>.
        /// </summary>
        /// <param name="ownerUserId">The user recorded when the task was created.</param>
        /// <param name="taskId">The opaque task identifier.</param>
        /// <param name="createdAt">Creation timestamp, UTC.</param>
        /// <param name="timeToLive">Duration from create, or <see langword="null"/> for unlimited.</param>
        /// <param name="pollIntervalMs">Client poll interval in milliseconds.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="ownerUserId"/> is <see langword="null"/> or whitespace.</exception>
        public StoredMcpTask(string ownerUserId, string taskId, DateTimeOffset createdAt, TimeSpan? timeToLive, long pollIntervalMs)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);

            OwnerUserId = ownerUserId;
            TaskId = taskId;
            Status = McpTaskStatus.Working;
            CreatedAt = createdAt;
            LastUpdatedAt = createdAt;
            var ttl = NormalizeTimeToLive(timeToLive);
            TimeToLive = ttl;
            if (ttl is { } duration)
            {
                ExpiresAt = createdAt + duration;
                DiscardAfter = createdAt + duration + duration;
            }

            PollIntervalMs = pollIntervalMs;
            InputRequests = EmptyInputRequests;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Copies protocol fields from <paramref name="info"/>, recomputes
        /// <see cref="ExpiresAt"/> and <see cref="DiscardAfter"/>, and caches the snapshot.
        /// Leaves <see cref="OwnerUserId"/> alone.
        /// </summary>
        /// <param name="info">The protocol-facing snapshot to store.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is <see langword="null"/>.</exception>
        public virtual void Apply(McpTaskInfo info)
        {
            ArgumentNullException.ThrowIfNull(info);

            TaskId = info.TaskId;
            Status = info.Status;
            CreatedAt = info.CreatedAt;
            LastUpdatedAt = info.LastUpdatedAt;
            TimeToLive = info.TimeToLive;
            var ttl = NormalizeTimeToLive(info.TimeToLive);
            if (ttl is { } duration)
            {
                ExpiresAt = info.CreatedAt + duration;
                DiscardAfter = info.CreatedAt + duration + duration;
            }
            else
            {
                ExpiresAt = null;
                DiscardAfter = null;
            }

            PollIntervalMs = info.PollIntervalMs;
            StatusMessage = info.StatusMessage;
            Result = info.Result;
            Error = info.Error;
            InputRequests = info.InputRequests is { Count: > 0 } ? info.InputRequests : EmptyInputRequests;
            _info = info;
        }

        /// <summary>
        /// Returns the protocol snapshot. Optional overrides allocate a new record and leave the cache alone.
        /// </summary>
        public virtual McpTaskInfo ToMcpTaskInfo(
            McpTaskStatus? status = null,
            DateTimeOffset? lastUpdatedAt = null,
            string? statusMessage = null,
            JsonElement? result = null,
            JsonElement? error = null,
            IReadOnlyDictionary<string, InputRequest>? inputRequests = null)
        {
            var hasOverride = status is not null
                || lastUpdatedAt is not null
                || statusMessage is not null
                || result is not null
                || error is not null
                || inputRequests is not null;

            if (!hasOverride && _info is not null)
            {
                return _info;
            }

            var info = new McpTaskInfo(
                TaskId,
                status ?? Status,
                CreatedAt,
                lastUpdatedAt ?? LastUpdatedAt,
                TimeToLive,
                PollIntervalMs,
                statusMessage ?? StatusMessage,
                result ?? Result,
                error ?? Error,
                inputRequests ?? InputRequests);

            if (!hasOverride)
            {
                _info = info;
            }

            return info;
        }

        /// <summary>
        /// Returns a snapshot with the same owner and a new <see cref="Info"/>.
        /// </summary>
        /// <param name="info">The protocol-facing snapshot to store.</param>
        /// <returns>A new snapshot with the same <see cref="OwnerUserId"/>.</returns>
        public virtual StoredMcpTask With(McpTaskInfo info)
        {
            var next = Clone();
            next.Apply(info);
            return next;
        }

        /// <summary>
        /// Returns a shallow copy for compare-and-swap updates. Mutate the copy with <c>Try*</c>;
        /// the original stays unchanged. Subclasses with extra mutable reference fields should override.
        /// </summary>
        public virtual StoredMcpTask Clone() => (StoredMcpTask)MemberwiseClone();

        /// <summary>
        /// Returns a mutated clone when this snapshot is still in progress and
        /// <paramref name="mutate"/> succeeds; otherwise this instance.
        /// </summary>
        /// <param name="mutate">Runs against a clone. Return <see langword="true"/> to keep it.</param>
        /// <returns>The clone when the mutation stuck; otherwise <c>this</c>.</returns>
        public virtual StoredMcpTask WithTry(Func<StoredMcpTask, bool> mutate)
        {
            if (IsFinished)
            {
                return this;
            }

            var next = Clone();
            return mutate(next) ? next : this;
        }

        /// <summary>
        /// Returns a writable copy of <see cref="InputRequests"/>, or <see langword="null"/> when empty.
        /// </summary>
        /// <returns>
        /// The live dictionary when this snapshot already owns one; otherwise a copy.
        /// Empty maps return <see langword="null"/> so JSON DTOs can omit the property.
        /// </returns>
        public virtual Dictionary<string, InputRequest>? ToInputRequestDictionaryOrNull()
        {
            if (InputRequests is not { Count: > 0 })
            {
                return null;
            }

            return InputRequests as Dictionary<string, InputRequest>
                ?? new Dictionary<string, InputRequest>(InputRequests, StringComparer.Ordinal);
        }

        /// <summary>
        /// Returns whether this task has reached <see cref="ExpiresAt"/> (or
        /// <c>CreatedAt + TimeToLive</c> when <see cref="ExpiresAt"/> is unset).
        /// </summary>
        /// <param name="now">The current UTC time.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="now"/> is at or past the work-window end.
        /// Work-window helper. Discard uses <see cref="HasExpired"/> / <see cref="DiscardAfter"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsExpired(DateTimeOffset now)
        {
            if (ExpiresAt is { } expires)
            {
                return now >= expires;
            }

            var ttl = TimeToLive;
            if (ttl is not { } duration || duration.Ticks <= 0)
            {
                return false;
            }

            return now.UtcTicks >= CreatedAt.UtcTicks + duration.Ticks;
        }

        /// <summary>
        /// Returns whether the task is still in its work window and may accept input or progress.
        /// </summary>
        /// <param name="now">The current UTC time.</param>
        /// <returns>
        /// <see langword="false"/> when the task is finished. Otherwise <see langword="true"/> when
        /// the task has no positive time-to-live, or when <paramref name="now"/> is still earlier
        /// than <c>CreatedAt + TimeToLive</c>.
        /// </returns>
        /// <remarks>
        /// Uses <see cref="TimeToLive"/>, not <see cref="ExpiresAt"/>. Settle (complete, fail,
        /// cancel) is still allowed after this returns <see langword="false"/> until
        /// <see cref="HasExpired"/> is <see langword="true"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsActive(DateTimeOffset now)
        {
            if (IsFinished)
            {
                return false;
            }

            var ttl = TimeToLive;
            if (ttl is not { } duration || duration.Ticks <= 0)
            {
                return true;
            }

            return now.UtcTicks - CreatedAt.UtcTicks < duration.Ticks;
        }

        /// <summary>
        /// Returns whether the snapshot has no remaining reads and should be discarded.
        /// </summary>
        /// <param name="now">The current UTC time.</param>
        /// <returns>
        /// <see langword="true"/> when <see cref="DiscardAfter"/> is set and
        /// <paramref name="now"/> is at or past it.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HasExpired(DateTimeOffset now)
            => DiscardAfter is { } deadline && now >= deadline;

        /// <summary>
        /// Returns whether <paramref name="userId"/> is the recorded owner.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsOwnedBy(string? userId)
            => !string.IsNullOrWhiteSpace(OwnerUserId)
                && !string.IsNullOrWhiteSpace(userId)
                && OwnerUserId == userId;

        /// <summary>
        /// Transitions to <see cref="McpTaskStatus.Cancelled"/> when the task is not finished.
        /// </summary>
        /// <returns><see langword="true"/> when the status changed.</returns>
        public virtual bool TryCancel(DateTimeOffset now)
        {
            if (IsFinished)
            {
                return false;
            }

            Status = McpTaskStatus.Cancelled;
            LastUpdatedAt = now;
            InvalidateInfo();
            return true;
        }

        /// <summary>
        /// Transitions to <see cref="McpTaskStatus.Completed"/> when the task is not finished.
        /// </summary>
        /// <returns><see langword="true"/> when the status changed.</returns>
        public virtual bool TryComplete(JsonElement result, DateTimeOffset now)
        {
            if (IsFinished)
            {
                return false;
            }

            Status = McpTaskStatus.Completed;
            Result = result;
            Error = null;
            LastUpdatedAt = now;
            InvalidateInfo();
            return true;
        }

        /// <summary>
        /// Transitions to <see cref="McpTaskStatus.Failed"/> when the task is not finished.
        /// </summary>
        /// <returns><see langword="true"/> when the status changed.</returns>
        public virtual bool TryFail(JsonElement error, DateTimeOffset now)
        {
            if (IsFinished)
            {
                return false;
            }

            Status = McpTaskStatus.Failed;
            Error = error;
            Result = null;
            LastUpdatedAt = now;
            InvalidateInfo();
            return true;
        }

        /// <summary>
        /// Merges <paramref name="inputRequests"/> and moves to <see cref="McpTaskStatus.InputRequired"/>
        /// when the task is not finished.
        /// </summary>
        /// <returns><see langword="true"/> when the snapshot changed.</returns>
        public virtual bool TrySetInputRequests(IDictionary<string, InputRequest> inputRequests, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(inputRequests);

            if (IsFinished)
            {
                return false;
            }

            var merged = InputRequests is { Count: > 0 }
                ? new Dictionary<string, InputRequest>(InputRequests, StringComparer.Ordinal)
                : new Dictionary<string, InputRequest>(inputRequests.Count, StringComparer.Ordinal);

            foreach (var pair in inputRequests)
            {
                merged[pair.Key] = pair.Value;
            }

            InputRequests = merged.Count == 0 ? EmptyInputRequests : merged;
            Status = McpTaskStatus.InputRequired;
            LastUpdatedAt = now;
            InvalidateInfo();
            return true;
        }

        /// <summary>
        /// Removes resolved ids from the outstanding map. Remaining requests stay
        /// <see cref="McpTaskStatus.InputRequired"/>; an empty map returns to <see cref="McpTaskStatus.Working"/>.
        /// </summary>
        /// <returns><see langword="true"/> when the snapshot changed.</returns>
        public virtual bool TryResolveInputRequests(IDictionary<string, InputResponse> inputResponses, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(inputResponses);

            if (IsFinished)
            {
                return false;
            }

            Dictionary<string, InputRequest>? remaining = null;
            if (InputRequests is { Count: > 0 })
            {
                remaining = new Dictionary<string, InputRequest>(InputRequests, StringComparer.Ordinal);
                foreach (var key in inputResponses.Keys)
                {
                    remaining.Remove(key);
                }
            }

            InputRequests = remaining is { Count: > 0 } ? remaining : EmptyInputRequests;
            Status = remaining is { Count: > 0 } ? McpTaskStatus.InputRequired : McpTaskStatus.Working;
            LastUpdatedAt = now;
            InvalidateInfo();
            return true;
        }

        /// <summary>
        /// Returns a positive TTL, or <see langword="null"/> when the task should be retained until deleted.
        /// </summary>
        public static TimeSpan? NormalizeTimeToLive(TimeSpan? timeToLive)
            => timeToLive is { } duration && duration > TimeSpan.Zero ? duration : null;

        /// <summary>
        /// Returns <paramref name="createdAt"/> plus <paramref name="timeToLive"/>, or <see langword="null"/> when unlimited.
        /// </summary>
        public static DateTimeOffset? ComputeExpiresAt(DateTimeOffset createdAt, TimeSpan? timeToLive)
        {
            var ttl = NormalizeTimeToLive(timeToLive);
            return ttl is { } duration ? createdAt + duration : null;
        }

        /// <summary>
        /// Returns when the row should be discarded, or <see langword="null"/> when unlimited.
        /// </summary>
        /// <param name="createdAt">Creation timestamp, UTC.</param>
        /// <param name="timeToLive">Duration from create, or <see langword="null"/> for unlimited.</param>
        /// <returns><c>createdAt + 2×timeToLive</c>, the visibility ceiling.</returns>
        public static DateTimeOffset? ComputeDiscardAfter(DateTimeOffset createdAt, TimeSpan? timeToLive)
        {
            var ttl = NormalizeTimeToLive(timeToLive);
            return ttl is { } duration ? createdAt + duration + duration : null;
        }

        /// <summary>
        /// Serializes <paramref name="element"/> to JSON text, or <see langword="null"/> when absent.
        /// </summary>
        public static string? ToJson(JsonElement? element)
        {
            if (element is not { } value || value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return null;
            }

            return value.GetRawText();
        }

        /// <summary>
        /// Parses JSON text into a detached <see cref="JsonElement"/>, or <see langword="null"/> when empty.
        /// </summary>
        public static JsonElement? ParseElement(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        /// <summary>
        /// Serializes an input-request map, or <see langword="null"/> when empty.
        /// </summary>
        public static string? ToInputRequestsJson(IReadOnlyDictionary<string, InputRequest>? requests)
        {
            if (requests is not { Count: > 0 })
            {
                return null;
            }

            var copy = requests as Dictionary<string, InputRequest>
                ?? new Dictionary<string, InputRequest>(requests, StringComparer.Ordinal);
            return JsonSerializer.Serialize(copy, McpTasksJsonContext.Default.DictionaryStringInputRequest);
        }

        /// <summary>
        /// Parses an input-request map. Empty or missing JSON returns <see cref="EmptyInputRequests"/>.
        /// </summary>
        public static IReadOnlyDictionary<string, InputRequest> ParseInputRequests(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return EmptyInputRequests;
            }

            var parsed = JsonSerializer.Deserialize(json, McpTasksJsonContext.Default.DictionaryStringInputRequest);
            if (parsed is not { Count: > 0 })
            {
                return EmptyInputRequests;
            }

            return parsed;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Drops the cached <see cref="McpTaskInfo"/> so the next <see cref="ToMcpTaskInfo"/> allocates a fresh record.
        /// </summary>
        protected virtual void InvalidateInfo() => _info = null;

        #endregion

    }

}
