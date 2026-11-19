using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Coverage for the persistence foundation: construct from <see cref="McpTaskInfo"/>,
    /// emit it back, and run the status machine on the task object.
    /// </summary>
    [TestClass]
    public class StoredMcpTaskTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_LoadsFromMcpTaskInfo()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var info = new McpTaskInfo("abc", McpTaskStatus.Working, now, now, TimeSpan.FromMinutes(5), 1000, "hi", null, null, StoredMcpTask.EmptyInputRequests);

            var stored = new StoredMcpTask("alice", info);

            stored.OwnerUserId.Should().Be("alice");
            stored.TaskId.Should().Be("abc");
            stored.Status.Should().Be(McpTaskStatus.Working);
            stored.ExpiresAt.Should().Be(now.AddMinutes(5));
            stored.DiscardAfter.Should().Be(now.AddMinutes(10));
            stored.Info.Should().BeSameAs(info);
            stored.ToMcpTaskInfo().Should().BeSameAs(info);
            stored.IsFinished.Should().BeFalse();
        }

        [TestMethod]
        public void WorkingConstructor_StampsDefaults()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var stored = new StoredMcpTask("alice", "task1", now, TimeSpan.FromSeconds(30), 250);

            stored.Status.Should().Be(McpTaskStatus.Working);
            stored.CreatedAt.Should().Be(now);
            stored.LastUpdatedAt.Should().Be(now);
            stored.PollIntervalMs.Should().Be(250);
            stored.TimeToLive.Should().Be(TimeSpan.FromSeconds(30));
            stored.ExpiresAt.Should().Be(now.AddSeconds(30));
            stored.DiscardAfter.Should().Be(now.AddSeconds(60));
            stored.IsOwnedBy("alice").Should().BeTrue();
            stored.IsOwnedBy("bob").Should().BeFalse();
        }

        [TestMethod]
        public void ToMcpTaskInfo_OverridesSpecifiedFields()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);
            var payload = JsonSerializer.SerializeToElement(new { ok = true });
            var error = JsonSerializer.SerializeToElement(new { e = 1 });
            var requests = new Dictionary<string, InputRequest> { ["r"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" }) };
            var updated = now.AddMinutes(1);

            var cloned = stored.ToMcpTaskInfo(McpTaskStatus.Failed, updated, "msg", payload, error, requests);

            cloned.Status.Should().Be(McpTaskStatus.Failed);
            cloned.LastUpdatedAt.Should().Be(updated);
            cloned.StatusMessage.Should().Be("msg");
            cloned.Result.Should().NotBeNull();
            cloned.Error.Should().NotBeNull();
            cloned.InputRequests.Should().ContainKey("r");
            stored.Info.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public void TryCancel_ThenComplete_IsNoOp()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);

            stored.TryCancel(now.AddSeconds(1)).Should().BeTrue();
            stored.Status.Should().Be(McpTaskStatus.Cancelled);
            stored.IsFinished.Should().BeTrue();
            stored.TryComplete(JsonSerializer.SerializeToElement(new { ok = true }), now.AddSeconds(2)).Should().BeFalse();
            stored.Status.Should().Be(McpTaskStatus.Cancelled);
            stored.Result.Should().BeNull();
        }

        [TestMethod]
        public void TryComplete_SetsResult_AndBlocksFail()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);
            var payload = JsonSerializer.SerializeToElement(new { n = 7 });

            stored.TryComplete(payload, now.AddSeconds(1)).Should().BeTrue();
            stored.Status.Should().Be(McpTaskStatus.Completed);
            stored.IsFinished.Should().BeTrue();
            stored.Result!.Value.GetProperty("n").GetInt32().Should().Be(7);
            stored.TryFail(JsonSerializer.SerializeToElement(new { e = 1 }), now.AddSeconds(2)).Should().BeFalse();
            stored.Error.Should().BeNull();
        }

        [TestMethod]
        public void TryFail_SetsError()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);
            var error = JsonSerializer.SerializeToElement(new { code = -1 });

            stored.TryFail(error, now.AddSeconds(1)).Should().BeTrue();
            stored.Status.Should().Be(McpTaskStatus.Failed);
            stored.IsFinished.Should().BeTrue();
            stored.Error!.Value.GetProperty("code").GetInt32().Should().Be(-1);
            stored.Result.Should().BeNull();
            stored.TryCancel(now.AddSeconds(2)).Should().BeFalse();
        }

        [TestMethod]
        public void TrySetAndResolveInputRequests_MovesStatus()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);
            var elicit = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" });

            stored.TrySetInputRequests(new Dictionary<string, InputRequest> { ["r1"] = elicit, ["r2"] = elicit }, now.AddSeconds(1)).Should().BeTrue();
            stored.Status.Should().Be(McpTaskStatus.InputRequired);
            stored.IsFinished.Should().BeFalse();
            stored.InputRequests.Should().HaveCount(2);

            stored.TryResolveInputRequests(new Dictionary<string, InputResponse> { ["r1"] = InputResponse.FromElicitResult(new ElicitResult()) }, now.AddSeconds(2)).Should().BeTrue();
            stored.Status.Should().Be(McpTaskStatus.InputRequired);
            stored.InputRequests.Should().ContainKey("r2");
            stored.InputRequests.Should().NotContainKey("r1");

            stored.TryResolveInputRequests(new Dictionary<string, InputResponse> { ["r2"] = InputResponse.FromElicitResult(new ElicitResult()) }, now.AddSeconds(3)).Should().BeTrue();
            stored.Status.Should().Be(McpTaskStatus.Working);
            stored.InputRequests.Should().BeSameAs(StoredMcpTask.EmptyInputRequests);
        }

        [TestMethod]
        public void TrySetInputRequests_WhenFinished_ReturnsFalse()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);
            stored.TryCancel(now).Should().BeTrue();

            stored.TrySetInputRequests(new Dictionary<string, InputRequest> { ["r"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "x" }) }, now).Should().BeFalse();
            stored.TryResolveInputRequests(new Dictionary<string, InputResponse>(), now).Should().BeFalse();
        }

        [TestMethod]
        public void Clone_DoesNotShareMutation()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);
            var clone = stored.Clone();

            clone.Should().NotBeSameAs(stored);
            clone.TryCancel(now.AddSeconds(1)).Should().BeTrue();
            stored.Status.Should().Be(McpTaskStatus.Working);
            stored.IsFinished.Should().BeFalse();
            clone.Status.Should().Be(McpTaskStatus.Cancelled);
            clone.IsFinished.Should().BeTrue();
        }

        [TestMethod]
        public void IsExpired_UsesExpiresAtThenTimeToLive()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "id", now, TimeSpan.FromSeconds(10), 1000);

            stored.IsExpired(now.AddSeconds(9)).Should().BeFalse();
            stored.IsExpired(now.AddSeconds(10)).Should().BeTrue();

            stored.ExpiresAt = null;
            stored.TimeToLive = TimeSpan.FromSeconds(5);
            stored.IsExpired(now.AddSeconds(5)).Should().BeTrue();

            stored.TimeToLive = null;
            stored.IsExpired(now.AddYears(1)).Should().BeFalse();
        }

        [TestMethod]
        public void IsActive_UsesWorkWindow_NotExpiresAt()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var ttl = TimeSpan.FromSeconds(10);
            var stored = new StoredMcpTask("alice", "id", now, ttl, 1000);

            stored.IsActive(now).Should().BeTrue();
            stored.IsActive(now.AddSeconds(9)).Should().BeTrue();
            stored.IsActive(now.AddSeconds(10)).Should().BeFalse();
            stored.IsActive(now.AddSeconds(15)).Should().BeFalse();

            stored.ExpiresAt = now;
            stored.IsActive(now.AddSeconds(1)).Should().BeTrue();
            stored.IsExpired(now.AddSeconds(1)).Should().BeTrue();

            stored.TryCancel(now.AddSeconds(1)).Should().BeTrue();
            stored.IsActive(now.AddSeconds(1)).Should().BeFalse();
        }

        [TestMethod]
        public void IsActive_UnlimitedOrNonPositiveTtl_IsTrueUntilFinished()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);

            stored.IsActive(now.AddYears(1)).Should().BeTrue();

            stored.TimeToLive = TimeSpan.Zero;
            stored.IsActive(now.AddYears(1)).Should().BeTrue();

            stored.TimeToLive = TimeSpan.FromSeconds(-1);
            stored.IsActive(now.AddYears(1)).Should().BeTrue();

            stored.TryComplete(JsonSerializer.SerializeToElement(new { ok = true }), now).Should().BeTrue();
            stored.IsActive(now).Should().BeFalse();
        }

        [TestMethod]
        public void HasExpired_WorkingTask_UsesTwoWindows()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var ttl = TimeSpan.FromSeconds(10);
            var stored = new StoredMcpTask("alice", "id", now, ttl, 1000);

            stored.DiscardAfter.Should().Be(now.AddSeconds(20));
            stored.HasExpired(now).Should().BeFalse();
            stored.HasExpired(now.AddSeconds(10)).Should().BeFalse();
            stored.HasExpired(now.AddSeconds(19)).Should().BeFalse();
            stored.HasExpired(now.AddSeconds(20)).Should().BeTrue();

            stored.ExpiresAt = now;
            stored.HasExpired(now.AddSeconds(10)).Should().BeFalse();
            stored.IsExpired(now).Should().BeTrue();
        }

        [TestMethod]
        public void HasExpired_DoesNotMoveWhenFinished()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "id", now, TimeSpan.FromSeconds(10), 1000);
            stored.TryComplete(JsonSerializer.SerializeToElement(new { ok = true }), now.AddSeconds(15)).Should().BeTrue();

            stored.DiscardAfter.Should().Be(now.AddSeconds(20));
            stored.HasExpired(now.AddSeconds(19)).Should().BeFalse();
            stored.HasExpired(now.AddSeconds(20)).Should().BeTrue();
            stored.IsExpired(now.AddSeconds(10)).Should().BeTrue();
        }

        [TestMethod]
        public void HasExpired_UnlimitedTtl_IsAlwaysFalse()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);

            stored.DiscardAfter.Should().BeNull();
            stored.HasExpired(now.AddYears(1)).Should().BeFalse();
            stored.TryCancel(now).Should().BeTrue();
            stored.DiscardAfter.Should().BeNull();
            stored.HasExpired(now.AddYears(1)).Should().BeFalse();
        }

        [TestMethod]
        public void IsFinished_IsTrueForCompletedCancelledFailed()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);

            stored.IsFinished.Should().BeFalse();
            stored.Status = McpTaskStatus.InputRequired;
            stored.IsFinished.Should().BeFalse();
            stored.Status = McpTaskStatus.Completed;
            stored.IsFinished.Should().BeTrue();
            stored.Status = McpTaskStatus.Cancelled;
            stored.IsFinished.Should().BeTrue();
            stored.Status = McpTaskStatus.Failed;
            stored.IsFinished.Should().BeTrue();
        }

        [TestMethod]
        public void JsonHelpers_RoundTripPayloads()
        {
            StoredMcpTask.ToJson(null).Should().BeNull();
            StoredMcpTask.ParseElement(null).Should().BeNull();
            StoredMcpTask.ParseElement("   ").Should().BeNull();
            StoredMcpTask.ParseInputRequests(null).Should().BeSameAs(StoredMcpTask.EmptyInputRequests);
            StoredMcpTask.ToInputRequestsJson(null).Should().BeNull();
            StoredMcpTask.ToInputRequestsJson(StoredMcpTask.EmptyInputRequests).Should().BeNull();

            var payload = JsonSerializer.SerializeToElement(new { ok = true, n = 3 });
            var json = StoredMcpTask.ToJson(payload);
            json.Should().NotBeNull();
            StoredMcpTask.ParseElement(json)!.Value.GetProperty("n").GetInt32().Should().Be(3);

            var requests = new Dictionary<string, InputRequest>
            {
                ["r1"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" })
            };
            var requestJson = StoredMcpTask.ToInputRequestsJson(requests);
            requestJson.Should().NotBeNull();
            var parsed = StoredMcpTask.ParseInputRequests(requestJson);
            parsed.Should().ContainKey("r1");
        }

        [TestMethod]
        public void NormalizeTimeToLive_AndComputeExpiresAt()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            StoredMcpTask.NormalizeTimeToLive(TimeSpan.FromSeconds(-1)).Should().BeNull();
            StoredMcpTask.NormalizeTimeToLive(null).Should().BeNull();
            StoredMcpTask.NormalizeTimeToLive(TimeSpan.FromSeconds(3)).Should().Be(TimeSpan.FromSeconds(3));
            StoredMcpTask.ComputeExpiresAt(now, null).Should().BeNull();
            StoredMcpTask.ComputeExpiresAt(now, TimeSpan.FromMinutes(2)).Should().Be(now.AddMinutes(2));
            StoredMcpTask.ComputeDiscardAfter(now, null).Should().BeNull();
            StoredMcpTask.ComputeDiscardAfter(now, TimeSpan.FromMinutes(2)).Should().Be(now.AddMinutes(4));
            StoredMcpTask.ComputeDiscardAfter(now, TimeSpan.Zero).Should().BeNull();
        }

        [TestMethod]
        public void Apply_ReplacesProtocolFields_KeepsOwner()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);
            var next = new McpTaskInfo("id", McpTaskStatus.Failed, now, now.AddSeconds(1), null, 1000, "boom", null, JsonSerializer.SerializeToElement(new { e = 1 }), StoredMcpTask.EmptyInputRequests);

            stored.Apply(next);

            stored.OwnerUserId.Should().Be("alice");
            stored.Status.Should().Be(McpTaskStatus.Failed);
            stored.IsFinished.Should().BeTrue();
            stored.StatusMessage.Should().Be("boom");
            stored.Info.Should().BeSameAs(next);
        }

        [TestMethod]
        public void Constructor_FromInfo_KeepsNonEmptyInputRequests()
        {
            var now = DateTimeOffset.UtcNow;
            var requests = new Dictionary<string, InputRequest>
            {
                ["r1"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" })
            };
            var info = new McpTaskInfo("abc", McpTaskStatus.InputRequired, now, now, null, 1000, null, null, null, requests);

            var stored = new StoredMcpTask("alice", info);

            stored.InputRequests.Should().ContainKey("r1");
        }

        [TestMethod]
        public void IsOwnedBy_WhenOwnerOrUserMissing_IsFalse()
        {
            var stored = new StoredMcpTask("alice", "id", DateTimeOffset.UtcNow, null, 1000);

            stored.IsOwnedBy(null).Should().BeFalse();
            stored.IsOwnedBy("  ").Should().BeFalse();
            stored.IsOwnedBy("alice").Should().BeTrue();
        }

        [TestMethod]
        public void TryResolveInputRequests_WhenMapEmpty_ReturnsToWorking()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);

            stored.TryResolveInputRequests(new Dictionary<string, InputResponse>
            {
                ["r1"] = InputResponse.FromElicitResult(new ElicitResult { Action = "accept" })
            }, now).Should().BeTrue();

            stored.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public void ToJson_WhenUndefinedOrNullKind_ReturnsNull()
        {
            StoredMcpTask.ToJson(default(JsonElement)).Should().BeNull();
            StoredMcpTask.ToJson(JsonSerializer.SerializeToElement((string?)null)).Should().BeNull();
        }

        [TestMethod]
        public void ToInputRequestsJson_WhenNotDictionary_Copies()
        {
            IReadOnlyDictionary<string, InputRequest> requests = new Dictionary<string, InputRequest>
            {
                ["r1"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" })
            }.AsReadOnly();

            var json = StoredMcpTask.ToInputRequestsJson(requests);
            json.Should().Contain("r1");
        }

        [TestMethod]
        public void ParseInputRequests_WhenEmptyObject_ReturnsEmpty()
        {
            StoredMcpTask.ParseInputRequests("{}").Should().BeSameAs(StoredMcpTask.EmptyInputRequests);
        }

        [TestMethod]
        public void WithTry_WhenFinished_ReturnsThis()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);
            stored.TryCancel(now).Should().BeTrue();

            stored.WithTry(next => next.TryComplete(JsonSerializer.SerializeToElement(new { ok = true }), now))
                .Should().BeSameAs(stored);
            stored.Status.Should().Be(McpTaskStatus.Cancelled);
        }

        [TestMethod]
        public void WithTry_WhenMutateFails_ReturnsThis()
        {
            var stored = new StoredMcpTask("alice", "id", DateTimeOffset.UtcNow, null, 1000);

            stored.WithTry(_ => false).Should().BeSameAs(stored);
            stored.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public void WithTry_WhenMutateSucceeds_ReturnsClone()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);

            var next = stored.WithTry(clone => clone.TryCancel(now));

            next.Should().NotBeSameAs(stored);
            next.Status.Should().Be(McpTaskStatus.Cancelled);
            stored.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public void ToInputRequestDictionaryOrNull_EmptyIsNull_CopiesNonDictionary()
        {
            var now = DateTimeOffset.UtcNow;
            var stored = new StoredMcpTask("alice", "id", now, null, 1000);

            stored.ToInputRequestDictionaryOrNull().Should().BeNull();

            var inner = new Dictionary<string, InputRequest>
            {
                ["r1"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" })
            };
            stored.TrySetInputRequests(inner, now);
            stored.ToInputRequestDictionaryOrNull().Should().BeSameAs(stored.InputRequests as Dictionary<string, InputRequest>);

            stored.InputRequests = new System.Collections.ObjectModel.ReadOnlyDictionary<string, InputRequest>(inner);
            var copy = stored.ToInputRequestDictionaryOrNull();
            copy.Should().ContainKey("r1");
            copy.Should().NotBeSameAs(inner);
        }

        [TestMethod]
        public void WorkingConstructor_DefersInfoUntilAccessed()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "task1", now, TimeSpan.FromSeconds(30), 250);

            var info = stored.Info;
            info.TaskId.Should().Be("task1");
            info.Status.Should().Be(McpTaskStatus.Working);
            stored.Info.Should().BeSameAs(info);
        }

        #endregion

    }

}
