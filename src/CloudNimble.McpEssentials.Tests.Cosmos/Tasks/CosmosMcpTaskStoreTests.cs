using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// Live Cosmos coverage. Skips when neither <c>COSMOS_CONNECTION_STRING</c>
    /// nor the emulator on localhost:8081 is available.
    /// </summary>
    [TestClass]
    public class CosmosMcpTaskStoreTests : BreakdanceMSTestBase
    {

        #region Fields

        private CosmosStoreTestContext? _cosmos;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            _cosmos = CosmosStoreTestContext.Create();
        }

        [TestCleanup]
        public async Task TearDown()
        {
            if (_cosmos is not null)
            {
                await _cosmos.DeleteDatabaseAsync();
                _cosmos.Dispose();
            }
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public async Task CreateTaskAsync_RecordsCurrentUser_AndOwnerCanGet()
        {
            var store = _cosmos!.CreateStore("alice");

            var created = await store.CreateTaskAsync();
            var fetched = await store.GetTaskAsync(created.TaskId);

            created.TaskId.Should().HaveLength(32);
            created.Status.Should().Be(McpTaskStatus.Working);
            created.PollIntervalMs.Should().Be(1000);
            created.TimeToLive.Should().BeNull();
            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
        }

        [TestMethod]
        public async Task CreateTaskAsync_IPerUser_UsesPrincipal()
        {
            var store = _cosmos!.CreateStore((string?)null);
            var alice = TestUser.Authenticated("alice");

            var created = await store.CreateTaskAsync(alice);
            var fetched = await store.GetTaskAsync(created.TaskId, alice);

            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
        }

        [TestMethod]
        public async Task PrincipalCtor_UsesClaims()
        {
            ClaimsPrincipal? current = TestUser.Authenticated("alice");
            var store = new CosmosMcpTaskStore(
                () => _cosmos!.Client,
                () => current is { } principal ? McpTaskStoreBase.GetUserId(principal) : null,
                null,
                _cosmos!.Gateway);
            _cosmos.Apply(store);

            var created = await store.CreateTaskAsync();
            current = TestUser.Authenticated("bob");

            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();

            current = TestUser.Authenticated("alice");
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenUnauthenticated_Throws()
        {
            var store = _cosmos!.CreateStore((string?)null);

            var act = async () => await store.CreateTaskAsync();

            await act.Should().ThrowAsync<McpTaskUserRequiredException>();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenDifferentUser_ReturnsNull()
        {
            var user = new TestUser("alice");
            var store = _cosmos!.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = "bob";
            var fetched = await store.GetTaskAsync(created.TaskId);

            fetched.Should().BeNull();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenUnknownId_ReturnsNull()
        {
            var store = _cosmos!.CreateStore("alice");

            (await store.GetTaskAsync("does-not-exist")).Should().BeNull();
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenOwner_Cancels()
        {
            var store = _cosmos!.CreateStore("alice");
            var created = await store.CreateTaskAsync();

            var cancelled = await store.SetCancelledAsync(created.TaskId);
            var fetched = await store.GetTaskAsync(created.TaskId);

            cancelled.Should().BeTrue();
            fetched!.Status.Should().Be(McpTaskStatus.Cancelled);
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenDifferentUser_ReturnsFalse_AndLeavesTaskWorking()
        {
            var user = new TestUser("alice");
            var store = _cosmos!.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = "bob";
            (await store.SetCancelledAsync(created.TaskId)).Should().BeFalse();

            user.UserId = "alice";
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public async Task SetCompletedAsync_WithoutCurrentUser_StillCompletes()
        {
            var user = new TestUser("alice");
            var store = _cosmos!.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();
            var payload = JsonPayload(new { secret = "alice-only" });

            user.UserId = null;
            await store.SetCompletedAsync(created.TaskId, payload);

            user.UserId = "alice";
            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Completed);
            fetched.Result!.Value.GetProperty("secret").GetString().Should().Be("alice-only");

            user.UserId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task SetCompletedAsync_WhenAlreadyTerminal_IsIdempotent()
        {
            var store = _cosmos!.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }));

            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Failed);
            fetched.Result.Should().BeNull();
        }

        [TestMethod]
        public async Task UsersCannotSeeEachOthersTasks_OrPayloads()
        {
            var user = new TestUser("alice");
            var store = _cosmos!.CreateStore(() => user.UserId);
            var aliceTask = await store.CreateTaskAsync();
            await store.SetCompletedAsync(aliceTask.TaskId, JsonPayload(new { secret = "alice" }));

            user.UserId = "bob";
            var bobTask = await store.CreateTaskAsync();

            (await store.GetTaskAsync(aliceTask.TaskId)).Should().BeNull();
            (await store.GetTaskAsync(bobTask.TaskId)).Should().NotBeNull();
            (await store.SetCancelledAsync(aliceTask.TaskId)).Should().BeFalse();

            user.UserId = "alice";
            (await store.GetTaskAsync(bobTask.TaskId)).Should().BeNull();
            var aliceFetched = await store.GetTaskAsync(aliceTask.TaskId);
            aliceFetched!.Result!.Value.GetProperty("secret").GetString().Should().Be("alice");
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_WhenOwner_RaisesEvent_AndReturnsToWorking()
        {
            var store = _cosmos!.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            var raised = 0;
            store.InputResponseReceived += args =>
            {
                raised++;
                args.TaskId.Should().Be(created.TaskId);
                args.RequestId.Should().Be("r1");
                args.Response.Should().NotBeNull();
            };

            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });
            await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            raised.Should().Be(1);
            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Working);
            fetched.InputRequests.Should().BeEmpty();
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_WhenDifferentUser_DoesNotRaiseEvent()
        {
            var user = new TestUser("alice");
            var store = _cosmos!.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();
            var raised = 0;
            store.InputResponseReceived += _ => raised++;

            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });

            user.UserId = "bob";
            await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            raised.Should().Be(0);

            user.UserId = "alice";
            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.InputRequired);
            fetched.InputRequests.Should().ContainKey("r1");
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenClockPastDiscardAfter_DeletesDocument()
        {
            var now = DateTimeOffset.UtcNow;
            var store = _cosmos!.CreateClockStore("alice", now);
            store.DefaultTimeToLive = TimeSpan.FromSeconds(10);
            var created = await store.CreateTaskAsync();

            store.Now = now.AddSeconds(20);
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task Complete_InSecondWindow_Succeeds()
        {
            var now = DateTimeOffset.UtcNow;
            var store = _cosmos!.CreateClockStore("alice", now);
            store.DefaultTimeToLive = TimeSpan.FromSeconds(10);
            store.ServeAndDrop = false;
            var created = await store.CreateTaskAsync();

            store.Now = now.AddSeconds(15);
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched.Should().NotBeNull();
            fetched!.Status.Should().Be(McpTaskStatus.Completed);
        }

        [TestMethod]
        public async Task SweepExpired_IsNoOp()
        {
            var now = DateTimeOffset.UtcNow;
            var store = _cosmos!.CreateClockStore("alice", now);
            store.DefaultTimeToLive = TimeSpan.FromSeconds(10);
            store.ServeAndDrop = false;
            var created = await store.CreateTaskAsync();

            store.Now = now.AddSeconds(20);
            await store.SweepExpiredAsync();

            store.Now = now.AddSeconds(5);
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetTaskAsync_TerminalSnapshot_ServeAndDrop()
        {
            var store = _cosmos!.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var first = await store.GetTaskAsync(created.TaskId);
            first.Should().NotBeNull();
            first!.Status.Should().Be(McpTaskStatus.Completed);
            first.Result!.Value.GetProperty("ok").GetBoolean().Should().BeTrue();

            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task GetTaskAsync_OtherUser_DoesNotServeAndDropOwnerRow()
        {
            var store = _cosmos!.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { secret = "alice" }));

            (await store.GetTaskAsync(created.TaskId, TestUser.Authenticated("bob"))).Should().BeNull();

            var owner = await store.GetTaskAsync(created.TaskId);
            owner.Should().NotBeNull();
            owner!.Status.Should().Be(McpTaskStatus.Completed);
            owner.Result!.Value.GetProperty("secret").GetString().Should().Be("alice");
        }

        [TestMethod]
        public async Task ConcurrentCreates_ProduceUniqueIds()
        {
            var store = _cosmos!.CreateStore("alice");
            var tasks = Enumerable.Range(0, 8).Select(_ => store.CreateTaskAsync()).ToArray();

            var created = await Task.WhenAll(tasks);

            created.Select(t => t.TaskId).Distinct().Should().HaveCount(8);
        }

        [TestMethod]
        public async Task ConcurrentSetInputRequests_DoNotLoseKeys()
        {
            var store = _cosmos!.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            var writes = Enumerable.Range(0, 8).Select(i =>
                store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
                {
                    [$"r{i}"] = CreateElicitRequest()
                }));

            await Task.WhenAll(writes);

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.InputRequests.Should().HaveCount(8);
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenCancelled_Throws()
        {
            var store = _cosmos!.CreateStore("alice");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await store.CreateTaskAsync(cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenTaskIdIsWhitespace_Throws()
        {
            var store = _cosmos!.CreateStore("alice");

            var act = async () => await store.GetTaskAsync("  ");

            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("taskId");
        }

        [TestMethod]
        public void AutoSweep_DefaultsToFalse()
        {
            var store = _cosmos!.CreateStore("alice");

            store.AutoSweep.Should().BeFalse();
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenIndexWriteFails_RollsBackTask()
        {
            _cosmos!.Gateway.FailNextIndex = true;
            var store = _cosmos.CreateStore("alice");

            var act = async () => await store.CreateTaskAsync();

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task SweepExpiredAsync_Completes()
        {
            var store = _cosmos!.CreateStore("alice");
            await store.SweepExpiredAsync();
        }

        [TestMethod]
        public void Inspectable_ExposesClientBusAndNewId()
        {
            var bus = new RecordingSignalBus();
            var store = new InspectableCosmosStore(() => _cosmos!.Client, () => "alice", _cosmos!.Gateway, () => bus);

            store.PublicClient.Should().BeSameAs(_cosmos.Client);
            store.PublicBus.Should().BeSameAs(bus);
            store.PublicNewTaskId().Should().HaveLength(32);
        }

        [TestMethod]
        public async Task Bus_SubscribesOnce_AndDisposeReleases()
        {
            var bus = new RecordingSignalBus();
            var store = new InspectableCosmosStore(() => _cosmos!.Client, () => "alice", _cosmos!.Gateway, () => bus);

            await store.CreateTaskAsync();
            await store.CreateTaskAsync();
            bus.SubscribeCount.Should().Be(1);

            store.Dispose();
            store.Dispose();
            bus.DisposeCount.Should().Be(1);
        }

        [TestMethod]
        public void PrincipalCtor_ThrowsWhenPrincipalFactoryIsNull()
        {
            Action act = () => _ = new CosmosMcpTaskStore(_cosmos!.Client, (Func<ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void StringCtor_AndPrincipalCtor_Construct()
        {
            using var client = CosmosStoreTestContext.CreateDummyClient();
            using var byId = new CosmosMcpTaskStore(client, "alice");
            using var byPrincipal = new CosmosMcpTaskStore(client, () => TestUser.Authenticated("alice"));

            byId.AutoSweep.Should().BeFalse();
            byPrincipal.AutoSweep.Should().BeFalse();
        }

        #endregion

        #region Private Methods

        private static JsonElement JsonPayload<T>(T value) => JsonSerializer.SerializeToElement(value);

        private static InputRequest CreateElicitRequest() =>
            InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" });

        private static InputResponse CreateElicitResponse() =>
            InputResponse.FromElicitResult(new ElicitResult { Action = "accept" });

        #endregion

    }

}
