using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.McpEssentials.Tests.AspNetCore
{

    /// <summary>
    /// In-memory Streamable HTTP MCP host: many concurrent clients, each with its own
    /// <c>X-User-Id</c>, must not see another user's tasks.
    /// </summary>
    [TestClass]
    public class ConcurrentUserIsolationTests : AuthenticatedMcpHost
    {

        #region Fields

        private const int UserCount = 32;

        #endregion

        #region Public Methods

        [TestMethod]
        [Timeout(120000)]
        public async Task ConcurrentUsers_DoNotCrossTaskStreams()
        {
            var users = Enumerable.Range(0, UserCount).Select(i => $"user-{i:00}").ToArray();
            var clients = users.Select(CreateUserClient).ToArray();

            try
            {
                var taskIds = await Task.WhenAll(clients.Select(CallHelloWorldAsTaskAsync));
                taskIds.Should().HaveCount(UserCount);
                taskIds.Should().OnlyHaveUniqueItems();
                taskIds.Should().AllSatisfy(id => id.Should().HaveLength(32));

                await Task.WhenAll(clients.Select((client, i) => AssertOwnerCanGetAsync(client, taskIds[i])));

                var crossGets = from owner in Enumerable.Range(0, UserCount)
                                from other in Enumerable.Range(0, UserCount)
                                where owner != other
                                select AssertStrangerCannotGetAsync(clients[other], taskIds[owner], users[other]);
                await Task.WhenAll(crossGets);

                var completed = await Task.WhenAll(clients.Select((client, i) =>
                    PollUntilCompletedAsync(client, taskIds[i], users[i])));
                completed.Should().HaveCount(UserCount);

                var afterComplete = from owner in Enumerable.Range(0, UserCount)
                                    from other in Enumerable.Range(0, UserCount)
                                    where owner != other
                                    select AssertStrangerCannotGetAsync(clients[other], taskIds[owner], users[other]);
                await Task.WhenAll(afterComplete);
            }
            finally
            {
                foreach (var client in clients)
                {
                    client.Dispose();
                }
            }
        }

        #endregion

        #region Private Methods

        private static async Task<string> CallHelloWorldAsTaskAsync(HttpClient client)
        {
            using var response = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload(McpStreamableHttp.HelloWorldToolName),
                "tools/call",
                McpStreamableHttp.HelloWorldToolName);
            var body = await McpStreamableHttp.ReadBodyAsync(response);
            response.IsSuccessStatusCode.Should().BeTrue("status {0} body {1}", (int)response.StatusCode, body);
            var taskId = McpStreamableHttp.ReadTaskId(body);
            taskId.Should().NotBeNullOrWhiteSpace("body {0}", body);
            return taskId!;
        }

        private static async Task AssertOwnerCanGetAsync(HttpClient client, string taskId)
        {
            using var response = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.TasksGetPayload(taskId),
                "tasks/get",
                taskId);
            var body = await McpStreamableHttp.ReadBodyAsync(response);
            response.IsSuccessStatusCode.Should().BeTrue("status {0} body {1}", (int)response.StatusCode, body);
            McpStreamableHttp.IsResult(body).Should().BeTrue("owner get failed: {0}", body);
            McpStreamableHttp.ReadTaskId(body).Should().Be(taskId);
        }

        private static async Task AssertStrangerCannotGetAsync(HttpClient client, string taskId, string stranger)
        {
            using var response = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.TasksGetPayload(taskId),
                "tasks/get",
                taskId);
            var body = await McpStreamableHttp.ReadBodyAsync(response);
            McpStreamableHttp.ReadTaskId(body).Should().NotBe(taskId, "{0} saw another user's task: {1}", stranger, body);
            if (McpStreamableHttp.IsResult(body))
            {
                McpStreamableHttp.ReadStatus(body).Should().NotBe("completed", "{0} received a completed stranger task: {1}", stranger, body);
            }
        }

        private static async Task<string> PollUntilCompletedAsync(HttpClient client, string taskId, string userId)
        {
            string? last = null;
            for (var attempt = 0; attempt < 200; attempt++)
            {
                using var response = await McpStreamableHttp.PostAsync(
                    client,
                    McpStreamableHttp.TasksGetPayload(taskId),
                    "tasks/get",
                    taskId);
                last = await McpStreamableHttp.ReadBodyAsync(response);
                if (string.Equals(McpStreamableHttp.ReadStatus(last), "completed", StringComparison.OrdinalIgnoreCase)
                    && last.Contains($"Hello, {userId}.", StringComparison.Ordinal))
                {
                    return last;
                }

                await Task.Delay(100);
            }

            throw new AssertFailedException($"Task {taskId} for {userId} never completed with that user's greeting. Last: {last}");
        }

        #endregion

    }

}
