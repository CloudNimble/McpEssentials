using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// Store coverage against an in-memory gateway: isolation, CAS, TTL, LISTEN/NOTIFY fan-out.
    /// </summary>
    [TestClass]
    public class PostgreSqlMcpTaskStoreTests : BreakdanceMSTestBase
    {

        #region Fields

        private PostgresStoreTestContext? _postgres;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup() => _postgres = new PostgresStoreTestContext();

        [TestCleanup]
        public void TearDown() => _postgres?.Dispose();

        #endregion

        #region Public Methods

        [TestMethod]
        public async Task CreateTaskAsync_RecordsCurrentUser_AndOwnerCanGet()
        {
            var store = _postgres!.CreateStore("alice");

            var created = await store.CreateTaskAsync();
            var fetched = await store.GetTaskAsync(created.TaskId);

            created.TaskId.Should().HaveLength(32);
            created.Status.Should().Be(McpTaskStatus.Working);
            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
            _postgres.Gateway.EnsureCalls.Should().BeGreaterThan(0);
            _postgres.Gateway.EnsuredTable.Should().Be("McpTasks");
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenDifferentUser_ReturnsNull()
        {
            var user = new TestUser("alice");
            var store = _postgres!.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task CreateTaskAsync_IPerUser_UsesPrincipal()
        {
            var store = _postgres!.CreateStore((string?)null);
            var alice = TestUser.Authenticated("alice");

            var created = await store.CreateTaskAsync(alice);
            var fetched = await store.GetTaskAsync(created.TaskId, alice);

            fetched.Should().NotBeNull();
        }

        [TestMethod]
        public async Task PrincipalCtor_UsesClaims()
        {
            ClaimsPrincipal? current = TestUser.Authenticated("alice");
            using var ctx = _postgres!;
            var store = new PostgreSqlMcpTaskStore(
                () => ctx.DataSource,
                () => current is { } principal ? McpTaskStoreBase.GetUserId(principal) : null,
                ctx.Gateway);

            var created = await store.CreateTaskAsync();
            current = TestUser.Authenticated("bob");
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenOwner_Cancels()
        {
            var store = _postgres!.CreateStore("alice");
            var created = await store.CreateTaskAsync();

            (await store.SetCancelledAsync(created.TaskId)).Should().BeTrue();
            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Cancelled);
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenDifferentUser_ReturnsFalse()
        {
            var user = new TestUser("alice");
            var store = _postgres!.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();
            user.UserId = "bob";

            (await store.SetCancelledAsync(created.TaskId)).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetCompletedAsync_ThenOwnerGet_ServesAndDrops()
        {
            var store = _postgres!.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Completed);
            fetched.Result!.Value.GetProperty("ok").GetBoolean().Should().BeTrue();
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task SetFailedAsync_RecordsError()
        {
            var store = _postgres!.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            store.ServeAndDrop = false;
            await store.SetFailedAsync(created.TaskId, JsonPayload(new { code = -1 }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Failed);
            fetched.Error!.Value.GetProperty("code").GetInt32().Should().Be(-1);
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_WhenOwner_RaisesEvent_AndNotifiesPeer()
        {
            var gateway = _postgres!.Gateway;
            var publisher = _postgres.CreateStore("alice");
            var subscriber = new PostgreSqlMcpTaskStore(() => _postgres.DataSource, () => "bob", gateway);
            var remote = new TaskCompletionSource<InputResponseReceivedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            subscriber.InputResponseReceived += args => remote.TrySetResult(args);

            var created = await publisher.CreateTaskAsync();
            await publisher.GetTaskAsync(created.TaskId);
            await subscriber.GetTaskAsync("missing");

            await publisher.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });
            await publisher.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            var args = await remote.Task.WaitAsync(TimeSpan.FromSeconds(2));
            args.TaskId.Should().Be(created.TaskId);
            args.RequestId.Should().Be("r1");
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_WhenDifferentUser_DoesNotRaiseEvent()
        {
            var user = new TestUser("alice");
            var store = _postgres!.CreateStore(() => user.UserId);
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
        }

        [TestMethod]
        public async Task SetInputRequestsAsync_WhenExpiredWorkWindow_IsNoOp()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var store = _postgres!.CreateClockStore("alice", now);
            store.DefaultTimeToLive = TimeSpan.FromSeconds(10);
            var created = await store.CreateTaskAsync();
            store.Now = now.AddSeconds(11);

            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });

            store.Now = now.AddSeconds(5);
            store.ServeAndDrop = false;
            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Working);
            fetched.InputRequests.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenPastDiscardAfter_IsMissing()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var store = _postgres!.CreateClockStore("alice", now);
            store.DefaultTimeToLive = TimeSpan.FromSeconds(10);
            var created = await store.CreateTaskAsync();
            store.Now = now.AddSeconds(20);

            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task SweepExpiredAsync_RemovesDiscardedRows()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var store = _postgres!.CreateClockStore("alice", now);
            store.DefaultTimeToLive = TimeSpan.FromSeconds(10);
            store.ServeAndDrop = false;
            var created = await store.CreateTaskAsync();
            store.Now = now.AddSeconds(20);
            await store.SweepExpiredAsync();

            store.Now = now;
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task TryApply_WhenVersionConflicts_Retries()
        {
            var store = _postgres!.CreateInspectable(() => "alice");
            var created = await store.CreateTaskAsync();
            _postgres.Gateway.BumpVersion(created.TaskId);

            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));
            store.ServeAndDrop = false;
            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Completed);
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenIdCollides_Retries()
        {
            var store = _postgres!.CreateInspectable(() => "alice");
            store.ForcedIds.Enqueue("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            store.ForcedIds.Enqueue("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            store.ForcedIds.Enqueue("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            await store.CreateTaskAsync();
            var second = await store.CreateTaskAsync();
            second.TaskId.Should().Be("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenCancelled_Throws()
        {
            var store = _postgres!.CreateStore("alice");
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var act = async () => await store.CreateTaskAsync(cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task SetCompletedAsync_WhenMissing_IsNoOp()
        {
            var store = _postgres!.CreateStore("alice");
            await store.SetCompletedAsync("missing", JsonPayload(new { ok = true }));
            await store.SetFailedAsync("missing", JsonPayload(new { e = 1 }));
            await store.SetInputRequestsAsync("missing", new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });
            (await store.GetTaskAsync("missing")).Should().BeNull();
            (await store.SetCancelledAsync("missing")).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenAlreadyCancelled_ReturnsFalse()
        {
            var store = _postgres!.CreateStore("alice");
            store.ServeAndDrop = false;
            var created = await store.CreateTaskAsync();
            (await store.SetCancelledAsync(created.TaskId)).Should().BeTrue();
            (await store.SetCancelledAsync(created.TaskId)).Should().BeFalse();
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenSchemaIsInvalid_Throws()
        {
            var store = _postgres!.CreateStore("alice");
            store.Schema = "mcp-bad";
            var act = async () => await store.CreateTaskAsync();
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenCancelled_Throws()
        {
            var store = _postgres!.CreateStore("alice");
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var act = async () => await store.GetTaskAsync("x", cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task Mapper_RoundTripsResultAndInputRequests()
        {
            var store = _postgres!.CreateStore("alice");
            store.ServeAndDrop = false;
            var created = await store.CreateTaskAsync();
            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { n = 7 }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Result!.Value.GetProperty("n").GetInt32().Should().Be(7);
        }

        [TestMethod]
        public void StringCtor_AndPrincipalCtor_Construct()
        {
            using var ctx = _postgres!;
            _ = new PostgreSqlMcpTaskStore(ctx.DataSource, "alice");
            _ = new PostgreSqlMcpTaskStore(ctx.DataSource, () => TestUser.Authenticated("alice"));
            _ = new PostgreSqlMcpTaskStore(ctx.DataSource, (Func<string?>)(() => "alice"));
        }

        [TestMethod]
        public void Constructor_WhenDataSourceIsNull_Throws()
        {
            Action act = () => _ = new PostgreSqlMcpTaskStore((NpgsqlDataSource)null!, "alice");
            act.Should().Throw<ArgumentNullException>().WithParameterName("dataSource");
        }

        [TestMethod]
        public void Constructor_WhenGetCurrentUserIdIsNull_Throws()
        {
            using var ctx = _postgres!;
            Action act = () => _ = new PostgreSqlMcpTaskStore(ctx.DataSource, (Func<string?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void Constructor_WhenGetPrincipalIsNull_Throws()
        {
            using var ctx = _postgres!;
            Action act = () => _ = new PostgreSqlMcpTaskStore(ctx.DataSource, (Func<ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void Gateway_WhenNoTestDouble_UsesNpgsqlAdapter()
        {
            using var ctx = _postgres!;
            var store = new PostgreSqlMcpTaskStore(ctx.DataSource, "alice");
            store.Gateway.Should().BeOfType<PostgreSqlTaskGateway>();
        }

        [TestMethod]
        public void PublicNewTaskId_HasLength32()
        {
            var store = _postgres!.CreateInspectable(() => "alice");
            store.PublicNewTaskId().Should().HaveLength(32);
            store.PublicGateway.Should().BeSameAs(_postgres.Gateway);
            store.PublicDataSource.Should().BeSameAs(_postgres.DataSource);
        }

        [TestMethod]
        public async Task Dispose_Unsubscribes()
        {
            var store = _postgres!.CreateStore("alice");
            await store.CreateTaskAsync();
            store.Dispose();
            store.Dispose();
        }

        [TestMethod]
        public async Task Notify_EmptyIds_AreIgnored()
        {
            var store = _postgres!.CreateStore("alice");
            await store.CreateTaskAsync();
            var raised = 0;
            store.InputResponseReceived += _ => raised++;
            await _postgres.Gateway.NotifyAsync("mcp_essentials_tasks", "", CancellationToken.None);
            await _postgres.Gateway.NotifyAsync("mcp_essentials_tasks", McpTaskInputSignal.Format("", "r1"), CancellationToken.None);
            await _postgres.Gateway.NotifyAsync("mcp_essentials_tasks", McpTaskInputSignal.Format("t", ""), CancellationToken.None);
            raised.Should().Be(0);
        }

        [TestMethod]
        public async Task Options_ApplyTo_CopiesTableAndChannel()
        {
            var store = _postgres!.CreateStore("alice");
            var options = new PostgreSqlMcpTaskStoreOptions
            {
                Schema = "mcp",
                TableName = "Tasks",
                ListenChannel = "mcp_tasks",
                AutoSweep = false
            };
            options.ApplyTo(store);
            store.Schema.Should().Be("mcp");
            store.TableName.Should().Be("Tasks");
            store.ListenChannel.Should().Be("mcp_tasks");
            store.AutoSweep.Should().BeFalse();
            await store.CreateTaskAsync();
            _postgres.Gateway.EnsuredSchema.Should().Be("mcp");
            _postgres.Gateway.EnsuredTable.Should().Be("Tasks");
        }

        [TestMethod]
        public void Options_ApplyTo_IgnoresNonPostgresStore()
        {
            var options = new PostgreSqlMcpTaskStoreOptions { TableName = "X" };
            options.ApplyTo(new InMemoryMcpTaskStorePlus("alice"));
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
