using CloudNimble.Breakdance.Extensions.MSTest2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Coverage for native-notification echo skip, subscribe-once, and dispose.
    /// </summary>
    [TestClass]
    public class NotifyingMcpTaskStoreBaseTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public async Task NotifyResolvedInput_PublishesAndSkipsOwnEcho()
        {
            using var store = new InspectableNotifyingStore("alice");
            var raised = 0;
            store.InputResponseReceived += _ => raised++;

            await store.PublicNotifyAsync("task-1", new Dictionary<string, InputResponse>
            {
                ["r1"] = InputResponse.FromElicitResult(new ElicitResult { Action = "accept" })
            });

            store.Subscribed.Should().BeTrue();
            store.Published.Should().ContainSingle(p => p.TaskId == "task-1" && p.RequestId == "r1");
            raised.Should().Be(1);
        }

        [TestMethod]
        public void EnsureSubscription_IsIdempotent()
        {
            using var store = new InspectableNotifyingStore("alice");
            store.PublicEnsure();
            store.PublicEnsure();
            store.Subscribed.Should().BeTrue();
        }

        [TestMethod]
        public void OnRemote_EmptyIds_AreIgnored()
        {
            using var store = new InspectableNotifyingStore("alice");
            var raised = 0;
            store.InputResponseReceived += _ => raised++;
            store.DeliverRemote(null, "r1");
            store.DeliverRemote("t", " ");
            store.DeliverRemote("", "r1");
            store.DeliverPayload(null);
            store.DeliverPayload("");
            store.DeliverPayload("no-newline");
            raised.Should().Be(0);
        }

        [TestMethod]
        public void OnRemote_RaisesForOtherNode()
        {
            using var store = new InspectableNotifyingStore("alice");
            var raised = 0;
            store.InputResponseReceived += args =>
            {
                raised++;
                args.TaskId.Should().Be("t1");
                args.RequestId.Should().Be("r1");
            };

            store.DeliverRemote("t1", "r1");
            raised.Should().Be(1);
        }

        [TestMethod]
        public async Task Dispose_WhenUnsubscribeThrows_DoesNotThrow()
        {
            var store = new InspectableNotifyingStore("alice");
            store.ThrowOnUnsubscribe = true;
            store.Dispose();
            store.Unsubscribed.Should().BeTrue();
            await Task.CompletedTask;
        }

        [TestMethod]
        public void PrincipalCtor_IsUsable()
        {
            using var store = new InspectableNotifyingStore(() => new System.Security.Claims.ClaimsPrincipal());
            store.Should().NotBeNull();
        }

        [TestMethod]
        public async Task PublishInputAsync_WhenCancelled_Throws()
        {
            using var store = new InspectableNotifyingStore("alice");
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var act = async () => await store.PublicNotifyAsync("t", new Dictionary<string, InputResponse>
            {
                ["r"] = InputResponse.FromElicitResult(new ElicitResult())
            }, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

    }

}
