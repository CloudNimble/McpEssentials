using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for the Tasks execution-mode wrap.
    /// </summary>
    [TestClass]
    public class FormElicitationExecutionModeTests
    {

        #region Public Methods

        [TestMethod]
        public void Apply_FormAndUrl_IsSynchronous()
        {
            var options = new McpTasksOptions
            {
                ExecutionModeSelector = _ => McpTaskExecutionMode.Required
            };
            FormElicitationExecutionMode.Apply(options);

            var primitive = new FakeMcpServerPrimitive
            {
                Metadata = [new McpElicitFormAttribute("ship"), new McpElicitUrlAttribute("connectUri")]
            };
            var context = CreateContext(primitive);

            options.ExecutionModeSelector!(context).Should().Be(McpTaskExecutionMode.Synchronous);
        }

        [TestMethod]
        public void Apply_FormTool_IsSynchronous()
        {
            var options = new McpTasksOptions
            {
                ExecutionModeSelector = _ => McpTaskExecutionMode.Required
            };
            FormElicitationExecutionMode.Apply(options);

            var primitive = new FakeMcpServerPrimitive
            {
                Metadata = [new McpElicitFormAttribute("ship")]
            };
            var context = CreateContext(primitive);

            options.ExecutionModeSelector!(context).Should().Be(McpTaskExecutionMode.Synchronous);
        }

        [TestMethod]
        public void Apply_NullOptions_Throws()
        {
            var act = () => FormElicitationExecutionMode.Apply(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [TestMethod]
        public void Apply_OtherTool_UsesInnerSelector()
        {
            var options = new McpTasksOptions
            {
                ExecutionModeSelector = _ => McpTaskExecutionMode.Required
            };
            FormElicitationExecutionMode.Apply(options);

            var context = CreateContext(new FakeMcpServerPrimitive { Metadata = [new object()] });
            options.ExecutionModeSelector!(context).Should().Be(McpTaskExecutionMode.Required);
        }

        [TestMethod]
        public void Apply_OtherTool_WhenInnerIsNull_IsOptional()
        {
            var options = new McpTasksOptions();
            options.ExecutionModeSelector = null!;
            FormElicitationExecutionMode.Apply(options);

            var context = CreateContext(new FakeMcpServerPrimitive());
            options.ExecutionModeSelector(context).Should().Be(McpTaskExecutionMode.Optional);
        }

        [TestMethod]
        public void Apply_UrlTool_IsOptional()
        {
            var options = new McpTasksOptions
            {
                ExecutionModeSelector = _ => McpTaskExecutionMode.Required
            };
            FormElicitationExecutionMode.Apply(options);

            var primitive = new FakeMcpServerPrimitive
            {
                Metadata = [new McpElicitUrlAttribute("connectUri")]
            };
            var context = CreateContext(primitive);

            options.ExecutionModeSelector!(context).Should().Be(McpTaskExecutionMode.Optional);
        }

        [TestMethod]
        public void IsFormTool_Context_Null_Throws()
        {
            var act = () => FormElicitationExecutionMode.IsFormTool((RequestContext<CallToolRequestParams>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("context");
        }

        [TestMethod]
        public void IsFormTool_Context_ReadsMatchedPrimitive()
        {
            var primitive = new FakeMcpServerPrimitive
            {
                Metadata = [new McpElicitFormAttribute("ship")]
            };
            FormElicitationExecutionMode.IsFormTool(CreateContext(primitive)).Should().BeTrue();
        }

        [TestMethod]
        public void IsFormTool_NullMetadata_IsFalse()
        {
            FormElicitationExecutionMode.IsFormTool(new FakeMcpServerPrimitive { Metadata = null! }).Should().BeFalse();
        }

        [TestMethod]
        public void IsFormTool_NullPrimitive_IsFalse()
        {
            FormElicitationExecutionMode.IsFormTool((IMcpServerPrimitive?)null).Should().BeFalse();
        }

        [TestMethod]
        public void IsFormTool_WithoutAttribute_IsFalse()
        {
            FormElicitationExecutionMode.IsFormTool(new FakeMcpServerPrimitive()).Should().BeFalse();
        }

        [TestMethod]
        public void IsUrlTool_Context_Null_Throws()
        {
            var act = () => FormElicitationExecutionMode.IsUrlTool((RequestContext<CallToolRequestParams>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("context");
        }

        [TestMethod]
        public void IsUrlTool_Context_ReadsMatchedPrimitive()
        {
            var primitive = new FakeMcpServerPrimitive
            {
                Metadata = [new McpElicitUrlAttribute("connectUri")]
            };
            FormElicitationExecutionMode.IsUrlTool(CreateContext(primitive)).Should().BeTrue();
        }

        [TestMethod]
        public void IsUrlTool_NullPrimitive_IsFalse()
        {
            FormElicitationExecutionMode.IsUrlTool((IMcpServerPrimitive?)null).Should().BeFalse();
        }

        [TestMethod]
        public void IsUrlTool_WithoutAttribute_IsFalse()
        {
            FormElicitationExecutionMode.IsUrlTool(new FakeMcpServerPrimitive()).Should().BeFalse();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Builds a request context with the given matched primitive.
        /// </summary>
        /// <param name="primitive">The matched tool.</param>
        /// <returns>A context suitable for selector tests.</returns>
        internal static RequestContext<CallToolRequestParams> CreateContext(IMcpServerPrimitive primitive)
        {
            var transport = new StreamServerTransport(new System.IO.MemoryStream(), new System.IO.MemoryStream(), "test", loggerFactory: null);
            var server = McpServer.Create(transport, new McpServerOptions(), loggerFactory: null, serviceProvider: null);
            var request = new JsonRpcRequest { Method = "tools/call" };
            return new RequestContext<CallToolRequestParams>(server, request, new CallToolRequestParams { Name = "x" })
            {
                MatchedPrimitive = primitive
            };
        }

        #endregion

    }

}
