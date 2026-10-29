using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Server;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Argument-guard coverage for <see cref="McpEssentials_Core_McpServerExtensions"/>.
    /// Live round-trips go through the SDK <c>ElicitAsync(ElicitRequestParams)</c> after <c>Build</c>.
    /// </summary>
    [TestClass]
    public class McpServerFormElicitationExtensionsTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void ElicitAsync_NullServer_Throws()
        {
            McpServer server = null!;
            var form = FormElicitation.Create("m");
            var act = () => server.ElicitAsync(form);
            act.Should().Throw<ArgumentNullException>().WithParameterName("server");
        }

        [TestMethod]
        public void ElicitAsync_NullForm_Throws()
        {
            McpServer server = null!;
            var act = () => server.ElicitAsync((IFormElicitationSource)null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void ElicitAsyncTyped_NullServer_Throws()
        {
            McpServer server = null!;
            var form = FormElicitation.From<EmptyForm>("m");
            var act = () => server.ElicitAsync(form);
            act.Should().Throw<ArgumentNullException>().WithParameterName("server");
        }

        [TestMethod]
        public void ElicitAsyncUntypedAsDto_NullForm_Throws()
        {
            McpServer server = null!;
            var act = () => server.ElicitAsync<BindTargetForm>((IFormElicitationSource)null!);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

    }

}
