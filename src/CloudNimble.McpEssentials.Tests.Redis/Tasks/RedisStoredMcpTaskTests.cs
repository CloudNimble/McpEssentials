using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Redis.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CloudNimble.McpEssentials.Tests.Redis.Tasks
{

    /// <summary>
    /// Coverage for the Redis JSON DTO, without a live engine.
    /// </summary>
    [TestClass]
    public class RedisStoredMcpTaskTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void From_WhenStoredIsNull_Throws()
        {
            Action act = () => RedisStoredMcpTask.From(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("stored");
        }

        [TestMethod]
        public void From_ToStored_RoundTripsEngineFields()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "abc", now, TimeSpan.FromSeconds(10), 1500);
            stored.TryComplete(JsonSerializer.SerializeToElement(new { ok = true }), now);

            var record = RedisStoredMcpTask.From(stored);
            var roundTripped = record.ToStored();

            roundTripped.TaskId.Should().Be("abc");
            roundTripped.OwnerUserId.Should().Be("alice");
            roundTripped.Status.Should().Be(McpTaskStatus.Completed);
            roundTripped.CreatedAt.Should().Be(now);
            roundTripped.TimeToLive.Should().Be(TimeSpan.FromSeconds(10));
            roundTripped.ExpiresAt.Should().Be(now.AddSeconds(10));
            roundTripped.DiscardAfter.Should().Be(now.AddSeconds(20));
            roundTripped.PollIntervalMs.Should().Be(1500);
            roundTripped.Result!.Value.GetProperty("ok").GetBoolean().Should().BeTrue();
        }

        [TestMethod]
        public void From_ToStored_RoundTripsInputRequests()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "abc", now, null, 1000);
            stored.TrySetInputRequests(new Dictionary<string, InputRequest>
            {
                ["r1"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" })
            }, now);

            var roundTripped = RedisStoredMcpTask.From(stored).ToStored();

            roundTripped.Status.Should().Be(McpTaskStatus.InputRequired);
            roundTripped.InputRequests.Should().ContainKey("r1");
        }

        [TestMethod]
        public void Json_RoundTripsCamelCaseStatusName()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "abc", now, TimeSpan.FromSeconds(10), 1000);
            var json = JsonSerializer.Serialize(RedisStoredMcpTask.From(stored), RedisMcpTasksJsonContext.Default.RedisStoredMcpTask);

            json.Should().Contain("\"status\":\"working\"");
            json.Should().Contain("\"taskId\":\"abc\"");
            json.Should().NotContain("\"info\"");
            json.Should().NotContain("\"isFinished\"");

            var record = JsonSerializer.Deserialize(json, RedisMcpTasksJsonContext.Default.RedisStoredMcpTask);
            record!.ToStored().OwnerUserId.Should().Be("alice");
        }

        #endregion

    }

}
