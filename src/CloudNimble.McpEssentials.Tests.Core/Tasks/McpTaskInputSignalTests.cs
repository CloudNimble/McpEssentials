using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Coverage for the native-channel payload codec.
    /// </summary>
    [TestClass]
    public class McpTaskInputSignalTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Format_JoinsWithANewline()
        {
            McpTaskInputSignal.Format("task-1", "req-1").Should().Be("task-1\nreq-1");
        }

        [TestMethod]
        public void TryParse_RoundTrips()
        {
            var payload = McpTaskInputSignal.Format("task-1", "req-1");
            McpTaskInputSignal.TryParse(payload, out var taskId, out var requestId).Should().BeTrue();
            taskId.Should().Be("task-1");
            requestId.Should().Be("req-1");
        }

        [TestMethod]
        public void TryParse_KeepsNewlinesInsideRequestId()
        {
            McpTaskInputSignal.TryParse("t1\nr1\nmore", out var taskId, out var requestId).Should().BeTrue();
            taskId.Should().Be("t1");
            requestId.Should().Be("r1\nmore");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("no-newline")]
        [DataRow("\nrequest")]
        [DataRow("task\n")]
        [DataRow("task\n ")]
        [DataRow(" \nrequest")]
        public void TryParse_WhenMalformed_ReturnsFalse(string? payload)
        {
            McpTaskInputSignal.TryParse(payload, out var taskId, out var requestId).Should().BeFalse();
            taskId.Should().BeNull();
            requestId.Should().BeNull();
        }

        #endregion

    }

}
