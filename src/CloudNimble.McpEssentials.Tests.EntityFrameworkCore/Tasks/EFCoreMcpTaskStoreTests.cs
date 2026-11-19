using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// SQLite-backed coverage for the EF Core authenticated task store.
    /// </summary>
    [TestClass]
    public class EFCoreMcpTaskStoreTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public async Task CreateTaskAsync_RecordsCurrentUser_AndOwnerCanGet()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");

            var created = await store.CreateTaskAsync();
            var fetched = await store.GetTaskAsync(created.TaskId);

            created.TaskId.Should().HaveLength(32);
            created.Status.Should().Be(McpTaskStatus.Working);
            created.PollIntervalMs.Should().Be(1000);
            created.TimeToLive.Should().BeNull();
            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
            db.CountRows().Should().Be(1);
        }

        [TestMethod]
        public async Task CreateTaskAsync_IPerUser_UsesPrincipal()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore((string?)null);
            var alice = TestUser.Authenticated("alice");

            var created = await store.CreateTaskAsync(alice);
            var fetched = await store.GetTaskAsync(created.TaskId, alice);

            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
        }

        [TestMethod]
        public async Task PrincipalCtor_UsesClaims()
        {
            using var db = new SqliteStoreTestContext();
            ClaimsPrincipal? current = TestUser.Authenticated("alice");
            var store = new EFCoreMcpTaskStore<McpTaskDbContext>(db.Scopes, () => current);

            var created = await store.CreateTaskAsync();
            current = TestUser.Authenticated("bob");

            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();

            current = TestUser.Authenticated("alice");
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenUnauthenticated_Throws()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore((string?)null);

            var act = async () => await store.CreateTaskAsync();

            await act.Should().ThrowAsync<McpTaskUserRequiredException>();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenDifferentUser_ReturnsNull()
        {
            using var db = new SqliteStoreTestContext();
            var user = new TestUser("alice");
            var store = db.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = "bob";
            var fetched = await store.GetTaskAsync(created.TaskId);

            fetched.Should().BeNull();
            db.CountRows().Should().Be(1);
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenUnknownId_ReturnsNull()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");

            (await store.GetTaskAsync("does-not-exist")).Should().BeNull();
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenOwner_Cancels()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            var created = await store.CreateTaskAsync();

            var cancelled = await store.SetCancelledAsync(created.TaskId);
            var fetched = await store.GetTaskAsync(created.TaskId);

            cancelled.Should().BeTrue();
            fetched!.Status.Should().Be(McpTaskStatus.Cancelled);
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenDifferentUser_ReturnsFalse_AndLeavesTaskWorking()
        {
            using var db = new SqliteStoreTestContext();
            var user = new TestUser("alice");
            var store = db.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = "bob";
            (await store.SetCancelledAsync(created.TaskId)).Should().BeFalse();

            user.UserId = "alice";
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenAlreadyTerminal_ReturnsFalse()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            (await store.SetCancelledAsync(created.TaskId)).Should().BeFalse();
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Completed);
        }

        [TestMethod]
        public async Task SetCompletedAsync_WithoutCurrentUser_StillCompletes()
        {
            using var db = new SqliteStoreTestContext();
            var user = new TestUser("alice");
            var store = db.CreateStore(() => user.UserId);
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
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }));

            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Failed);
            fetched.Result.Should().BeNull();
        }

        [TestMethod]
        public async Task SetFailedAsync_WithoutCurrentUser_StillFails()
        {
            using var db = new SqliteStoreTestContext();
            var user = new TestUser("alice");
            var store = db.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = null;
            await store.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }));

            user.UserId = "alice";
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Failed);
        }

        [TestMethod]
        public async Task UsersCannotSeeEachOthersTasks_OrPayloads()
        {
            using var db = new SqliteStoreTestContext();
            var user = new TestUser("alice");
            var store = db.CreateStore(() => user.UserId);
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
        public async Task ResolveInputRequestsAsync_WhenDifferentUser_DoesNotRaiseEvent()
        {
            using var db = new SqliteStoreTestContext();
            var user = new TestUser("alice");
            var store = db.CreateStore(() => user.UserId);
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
        public async Task ResolveInputRequestsAsync_WhenOwner_RaisesEvent_AndReturnsToWorking()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
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
        public async Task ResolveInputRequestsAsync_WhenOneOfTwoRemain_StaysInputRequired()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest(),
                ["r2"] = CreateElicitRequest()
            });

            await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.InputRequired);
            fetched.InputRequests.Should().ContainKey("r2");
            fetched.InputRequests.Should().NotContainKey("r1");
        }

        [TestMethod]
        public async Task SetInputRequestsAsync_MergesAndOverwrites()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            var first = CreateElicitRequest();
            var second = CreateElicitRequest();

            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = first
            });
            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = second,
                ["r2"] = first
            });

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.InputRequired);
            fetched.InputRequests.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task SetInputRequestsAsync_WhenTerminal_DoesNotChange()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCancelledAsync(created.TaskId);

            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Cancelled);
            fetched.InputRequests.Should().BeEmpty();
        }

        [TestMethod]
        public async Task DefaultTimeToLive_IsAppliedToNewTasks()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            store.DefaultTimeToLive = TimeSpan.FromMinutes(5);

            var created = await store.CreateTaskAsync();

            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
        }

        [TestMethod]
        public async Task ExpiredTask_IsRemovedOnGet()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            store.DefaultTimeToLive = TimeSpan.FromMilliseconds(1);
            var created = await store.CreateTaskAsync();

            await Task.Delay(30);
            var fetched = await store.GetTaskAsync(created.TaskId);

            fetched.Should().BeNull();
            db.CountRows().Should().Be(0);
        }

        [TestMethod]
        public async Task ConcurrentCreates_ProduceUniqueIds()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            var tasks = Enumerable.Range(0, 16).Select(_ => store.CreateTaskAsync()).ToArray();

            var created = await Task.WhenAll(tasks);

            created.Select(t => t.TaskId).Distinct().Should().HaveCount(16);
            db.CountRows().Should().Be(16);
        }

        [TestMethod]
        public async Task ConcurrentSetInputRequests_DoNotLoseKeys()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            var writes = Enumerable.Range(0, 12).Select(i =>
                store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
                {
                    [$"r{i}"] = CreateElicitRequest()
                }));

            await Task.WhenAll(writes);

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.InputRequests.Should().HaveCount(12);
        }

        [TestMethod]
        public async Task SignalBus_PublishesAfterOwnerResolve()
        {
            using var db = new SqliteStoreTestContext();
            var bus = new RecordingSignalBus();
            var store = db.CreateStore("alice", bus);
            var created = await store.CreateTaskAsync();
            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });

            await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            bus.Published.Should().ContainSingle();
            bus.Published[0].TaskId.Should().Be(created.TaskId);
            bus.Published[0].RequestId.Should().Be("r1");
        }

        [TestMethod]
        public async Task SignalBus_Deliver_RaisesLocalEvent()
        {
            using var db = new SqliteStoreTestContext();
            var bus = new RecordingSignalBus();
            var store = db.CreateStore("alice", bus);
            var created = await store.CreateTaskAsync();
            var raised = 0;
            store.InputResponseReceived += _ => raised++;

            // Subscribe happens on first operation.
            await store.GetTaskAsync(created.TaskId);
            bus.Deliver(created.TaskId, "r1");

            raised.Should().Be(1);
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenCancelled_Throws()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await store.CreateTaskAsync(cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenTaskIdIsWhitespace_Throws()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");

            var act = async () => await store.GetTaskAsync("  ");

            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("taskId");
        }

        [TestMethod]
        public void Constructor_WhenScopeFactoryIsNull_Throws()
        {
            Action act = () => _ = new EFCoreMcpTaskStore<McpTaskDbContext>(
                (IServiceScopeFactory)null!,
                "alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("scopeFactory");
        }

        [TestMethod]
        public void Constructor_WhenGetCurrentUserIdIsNull_Throws()
        {
            using var db = new SqliteStoreTestContext();

            Action act = () => _ = new EFCoreMcpTaskStore<McpTaskDbContext>(
                db.Scopes,
                (Func<string?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public async Task Mapper_RoundTripsResultAndInputRequests()
        {
            using var db = new SqliteStoreTestContext();
            var store = db.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true, n = 7 }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Result!.Value.GetProperty("ok").GetBoolean().Should().BeTrue();
            fetched.Result!.Value.GetProperty("n").GetInt32().Should().Be(7);
        }

        [TestMethod]
        public void Configuration_MapsMcpTasksTable()
        {
            using var db = new SqliteStoreTestContext();
            using var scope = db.Provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<McpTaskDbContext>();
            var entity = context.Model.FindEntityType(typeof(McpTaskEntity));

            entity.Should().NotBeNull();
            var mapped = entity!;
            mapped.GetTableName().Should().Be("McpTasks");
            var key = mapped.FindPrimaryKey();
            key.Should().NotBeNull();
            key!.Properties.Should().ContainSingle(p => p.Name == "TaskId");
            mapped.FindProperty("RowVersion")!.IsConcurrencyToken.Should().BeTrue();
            mapped.FindProperty("Status")!.GetProviderClrType().Should().Be(typeof(string));
            var discard = mapped.FindProperty("DiscardAfter");
            discard.Should().NotBeNull();
            discard!.GetValueConverter().Should().NotBeNull();
            discard.GetValueConverter()!.ProviderClrType.Should().Be(typeof(long?));
            mapped.GetIndexes().Should().Contain(i => i.Properties.Any(p => p.Name == "DiscardAfter"));
        }

        [TestMethod]
        public async Task CreateTaskAsync_StampsDiscardAfter()
        {
            using var db = new SqliteStoreTestContext();
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var store = new ClockEfStore(db.Scopes, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10),
                AutoSweep = false
            };

            await store.CreateTaskAsync();

            using var scope = db.Provider.CreateScope();
            var entity = scope.ServiceProvider.GetRequiredService<McpTaskDbContext>().Set<McpTaskEntity>().Single();
            entity.ExpiresAt.Should().Be(now.AddSeconds(10));
            entity.DiscardAfter.Should().Be(now.AddSeconds(20));
        }

        [TestMethod]
        public async Task SweepExpired_DeletesByDiscardAfter()
        {
            using var db = new SqliteStoreTestContext();
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var store = new ClockEfStore(db.Scopes, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10),
                AutoSweep = false
            };

            await store.CreateTaskAsync();

            store.Now = now.AddSeconds(19);
            await store.SweepExpiredAsync();
            db.CountRows().Should().Be(1);

            store.Now = now.AddSeconds(20);
            await store.SweepExpiredAsync();
            db.CountRows().Should().Be(0);
        }

        [TestMethod]
        public async Task SweepExpired_LeavesUnlimitedTtl()
        {
            using var db = new SqliteStoreTestContext();
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var store = new ClockEfStore(db.Scopes, "alice", now) { AutoSweep = false };

            await store.CreateTaskAsync();

            store.Now = now.AddYears(1);
            await store.SweepExpiredAsync();
            db.CountRows().Should().Be(1);
        }

        [TestMethod]
        public async Task Complete_DoesNotMoveDiscardAfter()
        {
            using var db = new SqliteStoreTestContext();
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var store = new ClockEfStore(db.Scopes, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10),
                AutoSweep = false,
                ServeAndDrop = false
            };

            var created = await store.CreateTaskAsync();
            store.Now = now.AddSeconds(15);
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            using var scope = db.Provider.CreateScope();
            var entity = scope.ServiceProvider.GetRequiredService<McpTaskDbContext>().Set<McpTaskEntity>().Single();
            entity.DiscardAfter.Should().Be(now.AddSeconds(20));
            entity.Status.Should().Be(McpTaskStatus.Completed);
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
