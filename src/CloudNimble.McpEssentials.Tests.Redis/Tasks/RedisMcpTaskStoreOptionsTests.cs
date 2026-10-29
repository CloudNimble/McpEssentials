using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Redis.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Redis.Tasks
{

    /// <summary>
    /// Coverage for Redis store options without a live engine.
    /// </summary>
    [TestClass]
    public class RedisMcpTaskStoreOptionsTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Ctor_DisablesAutoSweep()
        {
            var options = new RedisMcpTaskStoreOptions();

            options.AutoSweep.Should().BeFalse();
            options.KeyPrefix.Should().Be("mcp");
            options.WaitReplicas.Should().Be(0);
            options.WaitTimeout.Should().Be(TimeSpan.FromSeconds(1));
            options.Database.Should().Be(-1);
        }

        [TestMethod]
        public void ApplyTo_CopiesRedisKnobs()
        {
            var store = new RedisMcpTaskStore(() => throw new InvalidOperationException("unused"), () => "alice");
            var options = new RedisMcpTaskStoreOptions
            {
                DefaultTimeToLive = TimeSpan.FromMinutes(5),
                DefaultPollIntervalMs = 2500,
                AutoSweep = true,
                ServeAndDrop = false,
                KeyPrefix = "app",
                WaitReplicas = 2,
                WaitTimeout = TimeSpan.FromSeconds(3),
                Database = 4
            };

            options.ApplyTo(store);

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            store.AutoSweep.Should().BeTrue();
            store.ServeAndDrop.Should().BeFalse();
            store.KeyPrefix.Should().Be("app");
            store.WaitReplicas.Should().Be(2);
            store.WaitTimeout.Should().Be(TimeSpan.FromSeconds(3));
            store.Database.Should().Be(4);
        }

        [TestMethod]
        public void ApplyTo_WhenStoreIsNull_Throws()
        {
            var options = new RedisMcpTaskStoreOptions();

            Action act = () => options.ApplyTo(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("store");
        }

        #endregion

    }

}
