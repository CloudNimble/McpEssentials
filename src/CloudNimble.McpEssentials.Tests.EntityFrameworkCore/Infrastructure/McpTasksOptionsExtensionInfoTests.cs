using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.EntityFrameworkCore.Infrastructure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Infrastructure
{

    /// <summary>
    /// Coverage for EF options debug info and model cache keys.
    /// </summary>
    [TestClass]
    public class McpTasksOptionsExtensionInfoTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void LogFragment_IncludesSchemaWhenSet()
        {
            var withSchema = new McpTasksOptionsExtension("AppTasks", "mcp");
            withSchema.Info.LogFragment.Should().Contain("mcp.AppTasks");

            var without = new McpTasksOptionsExtension();
            without.Info.LogFragment.Should().Contain("McpTasksTable=McpTasks");
            without.Info.LogFragment.Should().NotContain(".");
        }

        [TestMethod]
        public void PopulateDebugInfo_WritesSchemaWhenSet()
        {
            var info = new McpTasksOptionsExtension("AppTasks", "mcp").Info;
            var debug = new Dictionary<string, string>();

            info.PopulateDebugInfo(debug);

            debug["McpEssentials:TableName"].Should().Be("AppTasks");
            debug["McpEssentials:Schema"].Should().Be("mcp");
        }

        [TestMethod]
        public void PopulateDebugInfo_WhenDebugInfoIsNull_Throws()
        {
            var info = new McpTasksOptionsExtension().Info;
            Action act = () => info.PopulateDebugInfo(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("debugInfo");
        }

        [TestMethod]
        public void ModelCacheKeyFactory_WhenContextIsNull_Throws()
        {
            var factory = new McpTasksModelCacheKeyFactory();
            Action act = () => factory.Create(null!, false);
            act.Should().Throw<ArgumentNullException>().WithParameterName("context");
        }

        [TestMethod]
        public void ShouldUseSameServiceProvider_MatchesInfoType()
        {
            var info = new McpTasksOptionsExtension().Info;
            info.ShouldUseSameServiceProvider(info).Should().BeTrue();
            info.IsDatabaseProvider.Should().BeFalse();
            info.GetServiceProviderHashCode().Should().Be(0);
        }

        #endregion

    }

}
