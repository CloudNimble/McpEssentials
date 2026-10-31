using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// Coverage for the Cosmos JSON DTO, without a live account.
    /// </summary>
    [TestClass]
    public class CosmosStoredMcpTaskTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void From_WhenStoredIsNull_Throws()
        {
            Action act = () => CosmosStoredMcpTask.From(null!, DateTimeOffset.UtcNow);

            act.Should().Throw<ArgumentNullException>().WithParameterName("stored");
        }

        [TestMethod]
        public void From_ToStored_RoundTripsEngineFields()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "abc", now, TimeSpan.FromSeconds(10), 1500);
            stored.TryComplete(JsonSerializer.SerializeToElement(new { ok = true }), now);

            var document = CosmosStoredMcpTask.From(stored, now);
            var roundTripped = document.ToStored();

            document.Id.Should().Be("abc");
            document.TimeToLiveSeconds.Should().Be(20);
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

            var roundTripped = CosmosStoredMcpTask.From(stored, now).ToStored();

            roundTripped.Status.Should().Be(McpTaskStatus.InputRequired);
            roundTripped.InputRequests.Should().ContainKey("r1");
        }

        [TestMethod]
        public void Json_RoundTripsCamelCaseStatusNameAndItemId()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "abc", now, TimeSpan.FromSeconds(10), 1000);
            var json = JsonSerializer.Serialize(CosmosStoredMcpTask.From(stored, now), CosmosMcpTasksJsonContext.Default.CosmosStoredMcpTask);

            json.Should().Contain("\"status\":\"working\"");
            json.Should().Contain("\"id\":\"abc\"");
            json.Should().Contain("\"ownerUserId\":\"alice\"");
            json.Should().Contain("\"ttl\":20");
            json.Should().NotContain("\"taskId\"");
            json.Should().NotContain("\"info\"");
            json.Should().NotContain("\"isFinished\"");

            var document = JsonSerializer.Deserialize(json, CosmosMcpTasksJsonContext.Default.CosmosStoredMcpTask);
            document!.ToStored().OwnerUserId.Should().Be("alice");
            document.TimeToLiveSeconds.Should().Be(20);
        }

        [TestMethod]
        public void ToTtlSeconds_WhenUnlimited_ReturnsNull()
        {
            CosmosStoredMcpTask.ToTtlSeconds(null, DateTimeOffset.UtcNow).Should().BeNull();
        }

        [TestMethod]
        public void ToTtlSeconds_WhenPastDeadline_ReturnsOne()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            CosmosStoredMcpTask.ToTtlSeconds(now.AddSeconds(-5), now).Should().Be(1);
        }

        [TestMethod]
        public void ToTtlSeconds_CeilsFractionalSeconds()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            CosmosStoredMcpTask.ToTtlSeconds(now.AddSeconds(10.1), now).Should().Be(11);
        }

        #endregion

    }

}
