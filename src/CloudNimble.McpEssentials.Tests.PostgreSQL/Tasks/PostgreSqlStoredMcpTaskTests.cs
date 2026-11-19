using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// Coverage for the Postgres row DTO.
    /// </summary>
    [TestClass]
    public class PostgreSqlStoredMcpTaskTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void From_WhenStoredIsNull_Throws()
        {
            Action act = () => PostgreSqlStoredMcpTask.From(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("stored");
        }

        [TestMethod]
        public void From_ToStored_RoundTripsEngineFields()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "abc", now, TimeSpan.FromSeconds(10), 1500);
            stored.TryComplete(JsonSerializer.SerializeToElement(new { ok = true }), now);

            var row = PostgreSqlStoredMcpTask.From(stored);
            var roundTripped = row.ToStored();

            roundTripped.TaskId.Should().Be("abc");
            roundTripped.OwnerUserId.Should().Be("alice");
            roundTripped.Status.Should().Be(McpTaskStatus.Completed);
            roundTripped.Result!.Value.GetProperty("ok").GetBoolean().Should().BeTrue();
            row.RowVersion.Should().Be(1);
        }

        [TestMethod]
        public void ToStored_WhenJsonEmpty_UsesEmptyInputRequests()
        {
            var row = new PostgreSqlStoredMcpTask
            {
                TaskId = "id",
                OwnerUserId = "alice",
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            var stored = row.ToStored();
            stored.InputRequests.Should().BeSameAs(StoredMcpTask.EmptyInputRequests);
            stored.Result.Should().BeNull();
        }

        [TestMethod]
        public void From_CopiesNonDictionaryInputRequests()
        {
            var now = DateTimeOffset.UtcNow;
            var inner = new Dictionary<string, InputRequest>
            {
                ["r1"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" })
            };
            var stored = new StoredMcpTask("alice", "abc", now, null, 1000);
            stored.TrySetInputRequests(inner, now);

            PostgreSqlStoredMcpTask.From(stored).InputRequestsJson.Should().Contain("r1");
        }

        #endregion

    }

}
