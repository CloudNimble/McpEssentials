using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// Coverage for Cosmos store options without a live account.
    /// </summary>
    [TestClass]
    public class CosmosMcpTaskStoreOptionsTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Ctor_DisablesAutoSweep()
        {
            var options = new CosmosMcpTaskStoreOptions();

            options.AutoSweep.Should().BeFalse();
            options.DatabaseId.Should().Be("mcpessentials");
            options.TasksContainerId.Should().Be("McpTasks");
            options.IndexContainerId.Should().Be("McpTaskIndex");
            options.Throughput.Should().Be(400);
        }

        [TestMethod]
        public void ApplyTo_CopiesCosmosKnobs()
        {
            var store = new CosmosMcpTaskStore(() => throw new InvalidOperationException("unused"), () => "alice");
            var options = new CosmosMcpTaskStoreOptions
            {
                DefaultTimeToLive = TimeSpan.FromMinutes(5),
                DefaultPollIntervalMs = 2500,
                AutoSweep = true,
                ServeAndDrop = false,
                DatabaseId = "app",
                TasksContainerId = "Tasks",
                IndexContainerId = "Index",
                Throughput = 1000
            };

            options.ApplyTo(store);

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            store.AutoSweep.Should().BeTrue();
            store.ServeAndDrop.Should().BeFalse();
            store.DatabaseId.Should().Be("app");
            store.TasksContainerId.Should().Be("Tasks");
            store.IndexContainerId.Should().Be("Index");
            store.Throughput.Should().Be(1000);
        }

        [TestMethod]
        public void ApplyTo_WhenStoreIsNull_Throws()
        {
            var options = new CosmosMcpTaskStoreOptions();

            Action act = () => options.ApplyTo(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("store");
        }

        [TestMethod]
        public void DefaultClientOptions_InstallsSystemTextJsonSerializer()
        {
            var options = CosmosMcpTaskStore.DefaultClientOptions();

            options.Serializer.Should().BeOfType<CosmosSystemTextJsonSerializer>();
        }

        #endregion

    }

}
