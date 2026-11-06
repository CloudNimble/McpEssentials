using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.McpEssentials.Tests.AspNetCore
{

    /// <summary>
    /// Streamable HTTP coverage for <c>[FormElicitation]</c> and <c>RequireForm</c>.
    /// </summary>
    [TestClass]
    public class FormElicitationRequestTests : FormElicitationHost
    {

        #region Public Methods

        [TestMethod]
        public async Task HelloWorld_WithTasksMeta_StillCreatesATask()
        {
            using var client = CreateUserClient("alice");
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload(McpStreamableHttp.HelloWorldToolName),
                "tools/call",
                McpStreamableHttp.HelloWorldToolName);
            var body = await McpStreamableHttp.ReadBodyAsync(call);

            McpStreamableHttp.ReadTaskId(body).Should().NotBeNullOrWhiteSpace(body);
        }

        [TestMethod]
        public async Task PlanTrip_CompletesAfterTwoRounds()
        {
            using var client = TestServer.CreateClient();
            using var first = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("PlanTrip", metaJson: McpStreamableHttp.ElicitationMetaJson),
                "tools/call",
                "PlanTrip");
            var firstBody = await McpStreamableHttp.ReadBodyAsync(first);
            McpStreamableHttp.ReadResultType(firstBody).Should().Be("input_required", firstBody);
            McpStreamableHttp.HasInputRequest(firstBody, "destination").Should().BeTrue(firstBody);

            using var second = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolRetryPayload(
                    "PlanTrip",
                    """{"destination":{"action":"accept","content":{"city":"Lisbon"}}}""",
                    metaJson: McpStreamableHttp.ElicitationMetaJson),
                "tools/call",
                "PlanTrip");
            var secondBody = await McpStreamableHttp.ReadBodyAsync(second);
            McpStreamableHttp.ReadResultType(secondBody).Should().Be("input_required", secondBody);
            McpStreamableHttp.HasInputRequest(secondBody, "dates").Should().BeTrue(secondBody);

            using var third = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolRetryPayload(
                    "PlanTrip",
                    """{"destination":{"action":"accept","content":{"city":"Lisbon"}},"dates":{"action":"accept","content":{"when":"June"}}}""",
                    metaJson: McpStreamableHttp.ElicitationMetaJson,
                    id: "3"),
                "tools/call",
                "PlanTrip");
            var thirdBody = await McpStreamableHttp.ReadBodyAsync(third);
            McpStreamableHttp.ReadText(thirdBody).Should().Be("Lisbon June", thirdBody);
        }

        [TestMethod]
        public async Task ShipBlank_ReturnsToolError()
        {
            using var client = TestServer.CreateClient();
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("ShipBlank", metaJson: McpStreamableHttp.ElicitationMetaJson),
                "tools/call",
                "ShipBlank");
            var body = await McpStreamableHttp.ReadBodyAsync(call);

            McpStreamableHttp.IsToolError(body).Should().BeTrue(body);
        }

        [TestMethod]
        public async Task ShipClass_FirstCall_ReturnsInputRequired()
        {
            using var client = TestServer.CreateClient();
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("ShipClass", metaJson: McpStreamableHttp.TasksAndElicitationMetaJson),
                "tools/call",
                "ShipClass");
            var body = await McpStreamableHttp.ReadBodyAsync(call);

            McpStreamableHttp.ReadResultType(body).Should().Be("input_required", body);
            McpStreamableHttp.HasInputRequest(body, "class-ship").Should().BeTrue(body);
            McpStreamableHttp.ReadTaskId(body).Should().BeNull(body);
        }

        [TestMethod]
        public async Task ShipFacade_RetryWithAccept_ReturnsTrackingNumber()
        {
            await AssertAcceptAsync("ShipFacade", "facade");
        }

        [TestMethod]
        public async Task ShipLive_RetryWithAccept_ReturnsTrackingNumber()
        {
            await AssertAcceptAsync("ShipLive", "live");
        }

        [TestMethod]
        public async Task ShipNameless_ReturnsToolError()
        {
            using var client = TestServer.CreateClient();
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("ShipNameless", metaJson: McpStreamableHttp.ElicitationMetaJson),
                "tools/call",
                "ShipNameless");
            var body = await McpStreamableHttp.ReadBodyAsync(call);

            McpStreamableHttp.IsToolError(body).Should().BeTrue(body);
        }

        [TestMethod]
        public async Task ShipOrder_Decline_ReturnsCancelled()
        {
            using var client = TestServer.CreateClient();
            using var first = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("ShipOrder", metaJson: McpStreamableHttp.ElicitationMetaJson),
                "tools/call",
                "ShipOrder");
            await McpStreamableHttp.ReadBodyAsync(first);

            using var retry = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolRetryPayload(
                    "ShipOrder",
                    """{"ship":{"action":"decline"}}"""),
                "tools/call",
                "ShipOrder");
            var body = await McpStreamableHttp.ReadBodyAsync(retry);

            McpStreamableHttp.ReadText(body).Should().Be("Shipping cancelled.", body);
        }

        [TestMethod]
        public async Task ShipOrder_FirstCall_ReturnsInputRequired()
        {
            using var client = TestServer.CreateClient();
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("ShipOrder", metaJson: McpStreamableHttp.ElicitationMetaJson),
                "tools/call",
                "ShipOrder");
            var body = await McpStreamableHttp.ReadBodyAsync(call);

            McpStreamableHttp.ReadResultType(body).Should().Be("input_required", body);
            McpStreamableHttp.HasInputRequest(body, "ship").Should().BeTrue(body);
            McpStreamableHttp.ReadTaskId(body).Should().BeNull(body);
        }

        [TestMethod]
        public async Task ShipOrder_MissingElicitationCapability_IsProtocolError()
        {
            using var client = TestServer.CreateClient();
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("ShipOrder"),
                "tools/call",
                "ShipOrder");
            var body = await McpStreamableHttp.ReadBodyAsync(call);

            body.Should().Contain("-32021", body);
        }

        [TestMethod]
        public async Task ShipOrder_RetryWithAccept_ReturnsTrackingNumber()
        {
            await AssertAcceptAsync("ShipOrder", "ship");
        }

        [TestMethod]
        public async Task ShipOrder_WithTasksMeta_StaysOnToolsCall()
        {
            using var client = CreateUserClient("alice");
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("ShipOrder", metaJson: McpStreamableHttp.TasksAndElicitationMetaJson),
                "tools/call",
                "ShipOrder");
            var body = await McpStreamableHttp.ReadBodyAsync(call);

            McpStreamableHttp.ReadResultType(body).Should().Be("input_required", body);
            McpStreamableHttp.ReadTaskId(body).Should().BeNull(body);
        }

        [TestMethod]
        public async Task ShipTyped_RetryWithAccept_ReturnsTrackingNumber()
        {
            await AssertAcceptAsync("ShipTyped", "typed");
        }

        [TestMethod]
        public async Task ShipTypedLive_RetryWithAccept_ReturnsTrackingNumber()
        {
            await AssertAcceptAsync("ShipTypedLive", "typed");
        }

        [TestMethod]
        public async Task ShipUnattributed_WithTasksMeta_FailsComposition()
        {
            using var client = CreateUserClient("alice");
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("ShipUnattributed", metaJson: McpStreamableHttp.TasksAndElicitationMetaJson),
                "tools/call",
                "ShipUnattributed");
            var body = await McpStreamableHttp.ReadBodyAsync(call);

            McpStreamableHttp.ReadTaskId(body).Should().NotBeNullOrWhiteSpace(body);
            McpStreamableHttp.ReadResultType(body).Should().Be("task", body);
        }

        [TestMethod]
        public async Task ShipUnknown_ReturnsToolError()
        {
            using var client = TestServer.CreateClient();
            using var call = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload("ShipUnknown", metaJson: McpStreamableHttp.ElicitationMetaJson),
                "tools/call",
                "ShipUnknown");
            var body = await McpStreamableHttp.ReadBodyAsync(call);

            McpStreamableHttp.IsToolError(body).Should().BeTrue(body);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// First-call MRTR plus accept retry for a one-field tracking form.
        /// </summary>
        /// <param name="toolName">The tool name.</param>
        /// <param name="formKey">The input-request key.</param>
        internal async Task AssertAcceptAsync(string toolName, string formKey)
        {
            using var client = TestServer.CreateClient();
            using var first = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolPayload(toolName, metaJson: McpStreamableHttp.ElicitationMetaJson),
                "tools/call",
                toolName);
            var firstBody = await McpStreamableHttp.ReadBodyAsync(first);
            McpStreamableHttp.ReadResultType(firstBody).Should().Be("input_required", firstBody);
            McpStreamableHttp.HasInputRequest(firstBody, formKey).Should().BeTrue(firstBody);

            using var retry = await McpStreamableHttp.PostAsync(
                client,
                McpStreamableHttp.CallToolRetryPayload(
                    toolName,
                    "{\"" + formKey + "\":{\"action\":\"accept\",\"content\":{\"trackingNumber\":\"1Z999\"}}}"),
                "tools/call",
                toolName);
            var body = await McpStreamableHttp.ReadBodyAsync(retry);
            McpStreamableHttp.ReadText(body).Should().Be("Shipped as 1Z999.", body);
        }

        #endregion

    }

}
