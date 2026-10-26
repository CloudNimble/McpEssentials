using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Coverage for <see cref="McpTaskUserRequiredException"/> constructors.
    /// </summary>
    [TestClass]
    public class McpTaskUserRequiredExceptionTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void DefaultConstructor_HasMessage()
        {
            var ex = new McpTaskUserRequiredException();

            ex.Message.Should().Contain("logged-in user");
        }

        [TestMethod]
        public void MessageConstructor_UsesMessage()
        {
            var ex = new McpTaskUserRequiredException("custom");

            ex.Message.Should().Be("custom");
        }

        [TestMethod]
        public void InnerExceptionConstructor_PreservesInner()
        {
            var inner = new InvalidOperationException("inner");
            var ex = new McpTaskUserRequiredException("custom", inner);

            ex.Message.Should().Be("custom");
            ex.InnerException.Should().BeSameAs(inner);
        }

        #endregion

    }

}
