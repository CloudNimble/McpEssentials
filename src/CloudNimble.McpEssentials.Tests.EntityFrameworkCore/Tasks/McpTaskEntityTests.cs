using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// Coverage for entity constructors and clone.
    /// </summary>
    [TestClass]
    public class McpTaskEntityTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Ctor_FromInfo_CopiesOwner()
        {
            var now = DateTimeOffset.UtcNow;
            var info = new McpTaskInfo("abc", McpTaskStatus.Working, now, now, null, 1000, null, null, null, StoredMcpTask.EmptyInputRequests);

            var entity = new McpTaskEntity("alice", info);

            entity.OwnerUserId.Should().Be("alice");
            entity.TaskId.Should().Be("abc");
        }

        [TestMethod]
        public void Clone_CopiesRowVersionBuffer()
        {
            var entity = new McpTaskEntity("alice", "abc", DateTimeOffset.UtcNow, null, 1000)
            {
                RowVersion = [1, 2, 3]
            };

            var clone = (McpTaskEntity)entity.Clone();

            clone.RowVersion.Should().Equal(1, 2, 3);
            clone.RowVersion.Should().NotBeSameAs(entity.RowVersion);
        }

        [TestMethod]
        public void Clone_WhenRowVersionEmpty_DoesNotCopyBuffer()
        {
            var entity = new McpTaskEntity("alice", "abc", DateTimeOffset.UtcNow, null, 1000)
            {
                RowVersion = []
            };

            var clone = (McpTaskEntity)entity.Clone();
            clone.RowVersion.Should().BeSameAs(entity.RowVersion);
        }

        [TestMethod]
        public void DefaultConfiguration_MapsMcpTasksTable()
        {
            var configuration = new McpTaskEntityConfiguration();
            configuration.TableName.Should().Be("McpTasks");
            configuration.Schema.Should().BeNull();
        }

        [TestMethod]
        public void JsonColumns_CacheUntilMutation()
        {
            var now = DateTimeOffset.UtcNow;
            var entity = new McpTaskEntity("alice", "abc", now, null, 1000);

            entity.ResultJson.Should().BeNull();
            entity.ResultJson.Should().BeNull();
            entity.ErrorJson.Should().BeNull();
            entity.InputRequestsJson.Should().BeNull();

            entity.TryComplete(JsonSerializer.SerializeToElement(new { ok = true }), now);
            var resultJson = entity.ResultJson;
            resultJson.Should().Contain("ok");
            entity.ResultJson.Should().BeSameAs(resultJson);

            entity.ResultJson = """{"n":1}""";
            entity.Result!.Value.GetProperty("n").GetInt32().Should().Be(1);
            entity.ResultJson.Should().Be("""{"n":1}""");

            var failed = new McpTaskEntity("alice", "ghi", now, null, 1000);
            failed.TryFail(JsonSerializer.SerializeToElement(new { e = 1 }), now);
            var errorJson = failed.ErrorJson;
            errorJson.Should().Contain("e");
            failed.ErrorJson.Should().BeSameAs(errorJson);
            failed.ErrorJson = """{"code":-1}""";
            failed.Error!.Value.GetProperty("code").GetInt32().Should().Be(-1);

            var working = new McpTaskEntity("alice", "def", now, null, 1000);
            working.TrySetInputRequests(new Dictionary<string, InputRequest>
            {
                ["r1"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" })
            }, now);
            var requestsJson = working.InputRequestsJson;
            requestsJson.Should().Contain("r1");
            working.InputRequestsJson.Should().BeSameAs(requestsJson);
            working.InputRequestsJson = requestsJson;
            working.InputRequests.Should().ContainKey("r1");
        }

        #endregion

    }

}
