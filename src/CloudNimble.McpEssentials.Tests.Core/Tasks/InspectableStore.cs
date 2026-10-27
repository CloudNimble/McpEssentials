using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Exposes in-memory store internals (clock, ids, CAS, TTL) for unit coverage.
    /// </summary>
    internal class InspectableStore : InMemoryAuthenticatedMcpTaskStore
    {

        #region Properties

        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public TimeSpan Sweep
        {
            get => SweepInterval;
            set => SweepInterval = value;
        }

        public Queue<string> ForcedIds { get; } = new();

        public ConcurrentDictionary<string, StoredMcpTask> Entries => Tasks;

        public Action? OnSweep { get; set; }

        public long SweepTicks
        {
            get => LastSweepTicks;
            set => LastSweepTicks = value;
        }

        #endregion

        #region Constructors

        public InspectableStore(string? userId)
            : base(userId)
        {
            SweepInterval = TimeSpan.Zero;
        }

        public InspectableStore(Func<string?> getCurrentUserId)
            : base(getCurrentUserId)
        {
            SweepInterval = TimeSpan.Zero;
        }

        #endregion

        #region Public Methods

        public ApplyResult Apply(string taskId, Func<StoredMcpTask, StoredMcpTask?> updater, out StoredMcpTask? applied, CancellationToken cancellationToken = default)
            => TryApply(taskId, updater, out applied, cancellationToken);

        public void SweepNow(CancellationToken cancellationToken = default)
            => SweepExpiredAsync(cancellationToken).GetAwaiter().GetResult();

        public void PulseNow(CancellationToken cancellationToken = default)
            => PulseSweepAsync(GetUtcNow(), cancellationToken).GetAwaiter().GetResult();

        public McpTaskInfo ClonePublic(
            McpTaskInfo source,
            McpTaskStatus? status = null,
            DateTimeOffset? lastUpdatedAt = null,
            string? statusMessage = null,
            JsonElement? result = null,
            JsonElement? error = null,
            IReadOnlyDictionary<string, InputRequest>? inputRequests = null)
            => new StoredMcpTask("clone", source).ToMcpTaskInfo(status, lastUpdatedAt, statusMessage, result, error, inputRequests);

        public TimeSpan? Normalize(TimeSpan? ttl) => NormalizeTimeToLive(ttl);

        public bool Expired(StoredMcpTask task, DateTimeOffset now) => task.IsExpired(now);

        public bool Active(StoredMcpTask task, DateTimeOffset now) => task.IsActive(now);

        public bool HasExpired(StoredMcpTask task, DateTimeOffset now) => task.HasExpired(now);

        public bool Discard(StoredMcpTask task, DateTimeOffset now) => ShouldDiscard(task, now);

        public bool OwnedBy(string? ownerUserId, string? currentUserId) => IsOwnedBy(ownerUserId, currentUserId);

        public string RequireCurrent() => RequireUserId();

        public string RequirePrincipal(ClaimsPrincipal user) => RequireUserId(user);

        #endregion

        #region Protected Methods

        protected override DateTimeOffset GetUtcNow() => Now;

        protected override Task SweepExpiredCoreAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            OnSweep?.Invoke();
            return base.SweepExpiredCoreAsync(now, cancellationToken);
        }

        protected override string NewTaskId()
            => ForcedIds.TryDequeue(out var id) ? id : base.NewTaskId();

        #endregion

    }

}
