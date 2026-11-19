using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for parking a fluent form on a task as a single input request.
    /// </summary>
    [TestClass]
    public class McpTaskStoreFormElicitationExtensionsTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public async Task SetInputRequests_Form_ParksUnderGeneratedId()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");
            var created = await store.CreateTaskAsync();
            var form = FormElicitationBuilder.Create("Filter?")
                .String("filter").Title("$filter")
                .Integer("top").Default(20);

            await store.SetInputRequestsAsync(created.TaskId, form);

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.InputRequired);
            var inputs = fetched.InputRequests;
            inputs.Should().NotBeNull();
            inputs.Should().HaveCount(1);
            var request = inputs!.Values.Should().ContainSingle().Subject;
            request.Method.Should().Be(RequestMethods.ElicitationCreate);
            var elicit = request.ElicitationParams;
            elicit.Should().NotBeNull();
            elicit!.Message.Should().Be("Filter?");
            elicit.RequestedSchema!.Properties.Should().ContainKeys("filter", "top");
        }

        [TestMethod]
        public async Task SetInputRequests_Form_UsesExplicitRequestId()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");
            var created = await store.CreateTaskAsync();
            var form = FormElicitationBuilder.From<EmptyForm>("Confirm?");

            await store.SetInputRequestsAsync(created.TaskId, "odata-confirm", form);

            var fetched = await store.GetTaskAsync(created.TaskId);
            var inputs = fetched!.InputRequests;
            inputs.Should().NotBeNull();
            inputs.Should().ContainKey("odata-confirm");
            inputs!["odata-confirm"].ElicitationParams!.Message.Should().Be("Confirm?");
        }

        [TestMethod]
        public async Task SetInputRequests_InputRequest_ParksSingle()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");
            var created = await store.CreateTaskAsync();
            var request = FormElicitationBuilder.Create("Need a name").String("name").Required().ToInputRequest();

            await store.SetInputRequestsAsync(created.TaskId, request);

            var fetched = await store.GetTaskAsync(created.TaskId);
            var inputs = fetched!.InputRequests;
            inputs.Should().NotBeNull();
            inputs.Should().HaveCount(1);
            inputs!.Keys.Should().AllSatisfy(id => id.Should().HaveLength(32));
        }

        [TestMethod]
        public async Task SetInputRequests_InputRequest_UsesExplicitRequestId()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");
            var created = await store.CreateTaskAsync();
            var request = FormElicitationBuilder.Create("Need a name").String("name").ToInputRequest();

            await store.SetInputRequestsAsync(created.TaskId, "name-field", request);

            (await store.GetTaskAsync(created.TaskId))!.InputRequests!.Should().ContainKey("name-field");
        }

        [TestMethod]
        public async Task SetInputRequests_NullForm_Throws()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");
            var created = await store.CreateTaskAsync();
            var act = () => store.SetInputRequestsAsync(created.TaskId, (IFormElicitationSource)null!);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("form");
        }

        [TestMethod]
        public async Task SetInputRequests_BlankRequestId_Throws()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");
            var created = await store.CreateTaskAsync();
            var form = FormElicitationBuilder.Create("m");
            var act = () => store.SetInputRequestsAsync(created.TaskId, "  ", form);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task SetInputRequests_NullStore_Throws()
        {
            IMcpTaskStore store = null!;
            var act = () => store.SetInputRequestsAsync("id", FormElicitationBuilder.Create("m").ToInputRequest());
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("store");
        }

        [TestMethod]
        public async Task SetInputRequests_InvalidForm_ThrowsBeforeStore()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");
            var created = await store.CreateTaskAsync();
            var form = FormElicitationBuilder.Create("m").Choice("color");
            var act = () => store.SetInputRequestsAsync(created.TaskId, form);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no options*");
        }

        #endregion

    }

}
