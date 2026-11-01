using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// Coverage for the LISTEN loop cancellation handle.
    /// </summary>
    [TestClass]
    public class PostgreSqlNotificationSubscriptionTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Dispose_CancelsAndRunsCallbackOnce()
        {
            var ran = 0;
            var subscription = new PostgreSqlNotificationSubscription(() => ran++);
            subscription.Token.CanBeCanceled.Should().BeTrue();
            var token = subscription.Token;

            subscription.Dispose();
            subscription.Dispose();

            ran.Should().Be(1);
            token.IsCancellationRequested.Should().BeTrue();
        }

        [TestMethod]
        public void Dispose_WhenCallbackThrows_StillDisposesToken()
        {
            var subscription = new PostgreSqlNotificationSubscription(() => throw new InvalidOperationException("boom"));
            Action act = subscription.Dispose;
            act.Should().Throw<InvalidOperationException>();
        }

        #endregion

    }

}
