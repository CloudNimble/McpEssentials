using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Coverage for registration options applied onto authenticated stores.
    /// </summary>
    [TestClass]
    public class McpTaskStoreOptionsTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void ApplyTo_CopiesTtlAndPollInterval()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");
            var options = new McpTaskStoreOptions
            {
                DefaultTimeToLive = TimeSpan.FromMinutes(5),
                DefaultPollIntervalMs = 2500
            };

            options.ApplyTo(store);

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public void ApplyTo_CopiesFlushKnobs()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");
            var options = new McpTaskStoreOptions
            {
                AutoSweep = false,
                SweepInterval = TimeSpan.FromMinutes(2),
                ServeAndDrop = false
            };

            options.ApplyTo(store);

            store.AutoSweep.Should().BeFalse();
            store.SweepInterval.Should().Be(TimeSpan.FromMinutes(2));
            store.ServeAndDrop.Should().BeFalse();
        }

        [TestMethod]
        public void ApplyTo_WhenStoreIsNull_Throws()
        {
            var options = new McpTaskStoreOptions();

            Action act = () => options.ApplyTo(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("store");
        }

        [TestMethod]
        public void Apply_WhenConfigureIsNull_LeavesDefaults()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");

            McpTaskStoreOptions.Apply(null, store);

            store.DefaultTimeToLive.Should().BeNull();
            store.DefaultPollIntervalMs.Should().Be(1000);
            store.AutoSweep.Should().BeTrue();
            store.SweepInterval.Should().Be(TimeSpan.FromSeconds(30));
            store.ServeAndDrop.Should().BeTrue();
        }

        [TestMethod]
        public void Apply_WhenStoreIsNull_Throws()
        {
            Action act = () => McpTaskStoreOptions.Apply(_ => { }, null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("store");
        }

        [TestMethod]
        public void Apply_InvokesConfigureAndCopies()
        {
            var store = new InMemoryMcpTaskStorePlus("alice");

            McpTaskStoreOptions.Apply(o =>
            {
                o.DefaultTimeToLive = TimeSpan.FromSeconds(30);
                o.DefaultPollIntervalMs = 500;
            }, store);

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromSeconds(30));
            store.DefaultPollIntervalMs.Should().Be(500);
        }

        #endregion

    }

}
