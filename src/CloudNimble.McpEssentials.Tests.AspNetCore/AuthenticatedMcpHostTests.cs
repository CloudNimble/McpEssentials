using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Server;

namespace CloudNimble.McpEssentials.Tests.AspNetCore
{

    /// <summary>
    /// Host-level checks for the sample MCP pipeline: capabilities, missing auth, and a single-user task.
    /// </summary>
    [TestClass]
    public class AuthenticatedMcpHostTests : AuthenticatedMcpHost
    {

        #region Public Methods

        [TestMethod]
        public async Task Host_RegistersHelloWorldTool()
        {
            var fromDi = TestServer.Services.GetServices<McpServerTool>()
                .Select(t => t.ProtocolTool.Name)
                .ToArray();

            using var client = TestServer.CreateClient();
            using var response = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.ToolsListPayload(),
                "tools/list");
            var body = await McpStreamableHttp.ReadBodyAsync(response);

            fromDi.Should().Contain(McpStreamableHttp.HelloWorldToolName, "DI tools: {0}; tools/list: {1}", string.Join(",", fromDi), body);
            body.Should().Contain(McpStreamableHttp.HelloWorldToolName);
        }

        [TestMethod]
        public async Task Initialize_AdvertisesTasksExtension()
        {
            using var client = TestServer.CreateClient();
            using var response = await McpStreamableHttp.InitializeAsync(client);
            var body = await McpStreamableHttp.ReadBodyAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue("status {0} body {1}", (int)response.StatusCode, body);
            body.Should().Contain("io.modelcontextprotocol/tasks");
        }

        [TestMethod]
        public async Task HelloWorld_WithoutUser_DoesNotCreateATask()
        {
            using var client = TestServer.CreateClient();
            using var response = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload(McpStreamableHttp.HelloWorldToolName),
                "tools/call",
                McpStreamableHttp.HelloWorldToolName);
            var body = await McpStreamableHttp.ReadBodyAsync(response);

            McpStreamableHttp.ReadTaskId(body).Should().BeNull("unauthenticated call created a task: {0}", body);
        }

        [TestMethod]
        public async Task HelloWorld_OwnerPollsToCompletion()
        {
            using var client = CreateUserClient("alice");
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload(McpStreamableHttp.HelloWorldToolName),
                "tools/call",
                McpStreamableHttp.HelloWorldToolName);
            var started = await McpStreamableHttp.ReadBodyAsync(call);
            var taskId = McpStreamableHttp.ReadTaskId(started);
            taskId.Should().NotBeNullOrWhiteSpace(started);

            string? last = null;
            for (var attempt = 0; attempt < 200; attempt++)
            {
                using var get = await McpStreamableHttp.PostAsync(
                    client,
                    McpStreamableHttp.TasksGetPayload(taskId!),
                    "tasks/get",
                    taskId);
                last = await McpStreamableHttp.ReadBodyAsync(get);
                if (McpStreamableHttp.ReadStatus(last) == "completed"
                    && last.Contains("Hello, alice.", System.StringComparison.Ordinal))
                {
                    return;
                }

                await Task.Delay(100);
            }

            Assert.Fail($"alice never saw a completed greeting. Last: {last}");
        }

        #endregion

    }

}
