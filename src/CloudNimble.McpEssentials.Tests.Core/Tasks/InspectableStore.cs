using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Exposes in-memory store internals (clock, ids, CAS, TTL) for unit coverage.
    /// </summary>
    internal class InspectableStore : InMemoryAuthenticatedMcpTaskStore
    {

        #region Properties

        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public TimeSpan Sweep { get; set; } = TimeSpan.Zero;

        public Queue<string> ForcedIds { get; } = new();

        public ConcurrentDictionary<string, StoredTask> Entries => Tasks;

        #endregion

        #region Constructors

        public InspectableStore(string? userId)
            : base(userId)
        {
        }

        public InspectableStore(Func<string?> getCurrentUserId)
            : base(getCurrentUserId)
        {
        }

        #endregion

        #region Public Methods

        public ApplyResult Apply(string taskId, Func<StoredTask, StoredTask?> updater, out StoredTask? applied)
            => TryApply(taskId, updater, out applied);

        public void SweepNow() => SweepExpired(GetUtcNow());

        public McpTaskInfo ClonePublic(
            McpTaskInfo source,
            McpTaskStatus? status = null,
            DateTimeOffset? lastUpdatedAt = null,
            string? statusMessage = null,
            JsonElement? result = null,
            JsonElement? error = null,
            IReadOnlyDictionary<string, InputRequest>? inputRequests = null)
            => Clone(source, status, lastUpdatedAt, statusMessage, result, error, inputRequests);

        public TimeSpan? Normalize(TimeSpan? ttl) => NormalizeTimeToLive(ttl);

        public bool Expired(McpTaskInfo info, DateTimeOffset now) => IsExpired(info, now);

        public bool OwnedBy(string? ownerUserId, string? currentUserId) => IsOwnedBy(ownerUserId, currentUserId);

        public string RequireCurrent() => RequireUserId();

        public string RequirePrincipal(ClaimsPrincipal user) => RequireUserId(user);

        #endregion

        #region Protected Methods

        protected override DateTimeOffset GetUtcNow() => Now;

        protected override TimeSpan SweepInterval => Sweep;

        protected override string NewTaskId()
            => ForcedIds.TryDequeue(out var id) ? id : base.NewTaskId();

        #endregion

    }

}
