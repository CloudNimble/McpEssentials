using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
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

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Unit coverage for owned in-memory storage, per-user isolation, and the
    /// <see cref="IPerUserMcpTaskStore"/> ClaimsPrincipal surface.
    /// </summary>
    [TestClass]
    public class InMemoryAuthenticatedMcpTaskStoreTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public async Task CreateTaskAsync_RecordsCurrentUser_AndOwnerCanGet()
        {
            var user = new TestUser("alice");
            var store = new InMemoryAuthenticatedMcpTaskStore(() => user.UserId);

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
            var store = new InMemoryAuthenticatedMcpTaskStore((string?)null);
            var alice = TestUser.Authenticated("alice");

            var created = await store.CreateTaskAsync(alice);
            var fetched = await store.GetTaskAsync(created.TaskId, alice);

            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
        }

        [TestMethod]
        public async Task StringCtor_UsesConstantUser()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");

            var created = await store.CreateTaskAsync();
            var fetched = await store.GetTaskAsync(created.TaskId);

            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
        }

        [TestMethod]
        public async Task PrincipalCtor_UsesClaims()
        {
            ClaimsPrincipal? current = TestUser.Authenticated("alice");
            var store = new InMemoryAuthenticatedMcpTaskStore(() => current);

            var created = await store.CreateTaskAsync();
            current = TestUser.Authenticated("bob");

            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();

            current = TestUser.Authenticated("alice");
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenUnauthenticated_Throws()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore((string?)null);

            var act = async () => await store.CreateTaskAsync();

            await act.Should().ThrowAsync<McpTaskUserRequiredException>();
        }

        [TestMethod]
        public async Task CreateTaskAsync_IPerUser_WhenUnauthenticated_Throws()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");

            var act = async () => await store.CreateTaskAsync(TestUser.Unauthenticated());

            await act.Should().ThrowAsync<McpTaskUserRequiredException>();
        }

        [TestMethod]
        public async Task CreateTaskAsync_IPerUser_WhenPrincipalIsNull_Throws()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");

            var act = async () => await store.CreateTaskAsync((ClaimsPrincipal)null!);

            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("user");
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenDifferentUser_ReturnsNull()
        {
            var user = new TestUser("alice");
            var store = new InMemoryAuthenticatedMcpTaskStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = "bob";
            var fetched = await store.GetTaskAsync(created.TaskId);

            fetched.Should().BeNull();
        }

        [TestMethod]
        public async Task GetTaskAsync_IPerUser_WhenDifferentUser_ReturnsNull_AndDoesNotRemove()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync(TestUser.Authenticated("alice"));

            var fetched = await inspect.GetTaskAsync(created.TaskId, TestUser.Authenticated("bob"));

            fetched.Should().BeNull();
            inspect.Entries.ContainsKey(created.TaskId).Should().BeTrue();
            inspect.Entries[created.TaskId].OwnerUserId.Should().Be("alice");
            inspect.Entries[created.TaskId].Info.Result.Should().BeNull();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenUnknownId_ReturnsNull()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");

            var fetched = await store.GetTaskAsync("does-not-exist");

            fetched.Should().BeNull();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenUnauthenticated_Throws()
        {
            var user = new TestUser("alice");
            var store = new InMemoryAuthenticatedMcpTaskStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = null;
            var act = async () => await store.GetTaskAsync(created.TaskId);

            await act.Should().ThrowAsync<McpTaskUserRequiredException>();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenTaskIdIsWhitespace_Throws()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");

            var act = async () => await store.GetTaskAsync("  ");

            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("taskId");
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenOwner_Cancels()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();

            var cancelled = await store.SetCancelledAsync(created.TaskId);
            var fetched = await store.GetTaskAsync(created.TaskId);

            cancelled.Should().BeTrue();
            fetched!.Status.Should().Be(McpTaskStatus.Cancelled);
        }

        [TestMethod]
        public async Task SetCancelledAsync_IPerUser_WhenOwner_Cancels()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore((string?)null);
            var alice = TestUser.Authenticated("alice");
            var created = await store.CreateTaskAsync(alice);

            var cancelled = await store.SetCancelledAsync(created.TaskId, alice);

            cancelled.Should().BeTrue();
            (await store.GetTaskAsync(created.TaskId, alice))!.Status.Should().Be(McpTaskStatus.Cancelled);
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenDifferentUser_ReturnsFalse_AndLeavesTaskWorking()
        {
            var user = new TestUser("alice");
            var store = new InMemoryAuthenticatedMcpTaskStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = "bob";
            var cancelled = await store.SetCancelledAsync(created.TaskId);

            cancelled.Should().BeFalse();

            user.UserId = "alice";
            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenMissing_ReturnsFalse()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");

            (await store.SetCancelledAsync("missing")).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenAlreadyTerminal_ReturnsFalse()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            (await store.SetCancelledAsync(created.TaskId)).Should().BeFalse();
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Completed);
        }

        [TestMethod]
        public async Task SetCompletedAsync_WithoutCurrentUser_StillCompletes()
        {
            var user = new TestUser("alice");
            var store = new InMemoryAuthenticatedMcpTaskStore(() => user.UserId);
            var created = await store.CreateTaskAsync();
            var payload = JsonPayload(new { secret = "alice-only" });

            user.UserId = null;
            await store.SetCompletedAsync(created.TaskId, payload);

            user.UserId = "alice";
            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Completed);
            fetched.Result.Should().NotBeNull();
            fetched.Result!.Value.GetProperty("secret").GetString().Should().Be("alice-only");

            user.UserId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task SetCompletedAsync_WhenMissing_DoesNotThrow()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");

            var act = async () => await store.SetCompletedAsync("missing", JsonPayload(true));

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task SetCompletedAsync_WhenAlreadyTerminal_IsIdempotent()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }));

            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Failed);
            fetched.Result.Should().BeNull();
        }

        [TestMethod]
        public async Task SetFailedAsync_WhenAlreadyTerminal_IsIdempotent()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            await store.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Completed);
            fetched.Error.Should().BeNull();
        }

        [TestMethod]
        public async Task SetFailedAsync_WithoutCurrentUser_StillFails()
        {
            var user = new TestUser("alice");
            var store = new InMemoryAuthenticatedMcpTaskStore(() => user.UserId);
            var created = await store.CreateTaskAsync();
            var error = JsonPayload(new { message = "boom" });

            user.UserId = null;
            await store.SetFailedAsync(created.TaskId, error);

            user.UserId = "alice";
            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Failed);
            fetched.Error.Should().NotBeNull();
        }

        [TestMethod]
        public async Task UsersCannotSeeEachOthersTasks_OrPayloads()
        {
            var user = new TestUser("alice");
            var store = new InMemoryAuthenticatedMcpTaskStore(() => user.UserId);
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
            aliceFetched.Should().NotBeNull();
            aliceFetched!.Result!.Value.GetProperty("secret").GetString().Should().Be("alice");
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_WhenDifferentUser_DoesNotRaiseEvent()
        {
            var user = new TestUser("alice");
            var store = new InMemoryAuthenticatedMcpTaskStore(() => user.UserId);
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
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
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
        public async Task ResolveInputRequestsAsync_IPerUser_WhenOwner_RaisesEvent()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore((string?)null);
            var alice = TestUser.Authenticated("alice");
            var created = await store.CreateTaskAsync(alice);
            var raised = 0;
            store.InputResponseReceived += _ => raised++;

            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });
            await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            }, alice);

            raised.Should().Be(1);
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_WhenOneOfTwoRemain_StaysInputRequired()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
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
        public async Task ResolveInputRequestsAsync_WhenInputRequestsIsNull_TreatsAsEmpty()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();
            inspect.Entries[created.TaskId] = new StoredMcpTask(
                "alice",
                new McpTaskInfo(
                    created.TaskId,
                    created.Status,
                    created.CreatedAt,
                    created.LastUpdatedAt,
                    created.TimeToLive,
                    created.PollIntervalMs,
                    created.StatusMessage,
                    created.Result,
                    created.Error,
                    null!));

            await inspect.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            var fetched = await inspect.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_WhenNoPendingRequests_StaysWorking()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();

            await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Working);
            fetched.InputRequests.Should().BeEmpty();
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_WhenMissing_DoesNotRaise()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var raised = 0;
            store.InputResponseReceived += _ => raised++;

            await store.ResolveInputRequestsAsync("missing", new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            raised.Should().Be(0);
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_Unsubscribe_DoesNotInvoke()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();
            var raised = 0;
            void Handler(InputResponseReceivedEventArgs _) => raised++;
            store.InputResponseReceived += Handler;
            store.InputResponseReceived -= Handler;

            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });
            await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            raised.Should().Be(0);
        }

        [TestMethod]
        public async Task SetInputRequestsAsync_MergesAndOverwrites()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
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
            fetched.InputRequests!["r1"].Should().BeSameAs(second);
        }

        [TestMethod]
        public async Task SetInputRequestsAsync_WhenEmpty_TransitionsToInputRequired()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();

            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>());

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.InputRequired);
            fetched.InputRequests.Should().BeEmpty();
        }

        [TestMethod]
        public async Task SetInputRequestsAsync_WhenTerminal_DoesNotChange()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
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
        public async Task SetInputRequestsAsync_WhenNull_Throws()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();

            var act = async () => await store.SetInputRequestsAsync(created.TaskId, null!);

            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("inputRequests");
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_WhenNull_Throws()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();

            var act = async () => await store.ResolveInputRequestsAsync(created.TaskId, null!);

            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("inputResponses");
        }

        [TestMethod]
        public void Constructor_WhenGetCurrentUserIdIsNull_Throws()
        {
            Action act = () => _ = new InMemoryAuthenticatedMcpTaskStore((Func<string?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void Constructor_WhenGetPrincipalIsNull_Throws()
        {
            Action act = () => _ = new InMemoryAuthenticatedMcpTaskStore((Func<ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public async Task Subclass_CanOverrideCreate()
        {
            var store = new CountingStore();

            await store.CreateTaskAsync();

            store.Creates.Should().Be(1);
        }

        [TestMethod]
        public void Type_IsPerUserStore_NotSdkWrapper()
        {
            typeof(InMemoryAuthenticatedMcpTaskStore).BaseType.Should().Be(typeof(AuthenticatedMcpTaskStoreBase));
            typeof(IPerUserMcpTaskStore).GetInterfaces().Should().Contain(typeof(IMcpTaskStore));
            typeof(InMemoryAuthenticatedMcpTaskStore).GetProperty("Inner").Should().BeNull();
            typeof(InMemoryAuthenticatedMcpTaskStore).GetInterfaces().Should().Contain(typeof(IPerUserMcpTaskStore));
            typeof(IPerUserMcpTaskStore).GetMethod(nameof(IPerUserMcpTaskStore.SweepExpiredAsync)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task DefaultPollIntervalMs_IsAppliedToNewTasks()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice")
            {
                DefaultPollIntervalMs = 2500
            };

            var created = await store.CreateTaskAsync();

            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public async Task DefaultTimeToLive_IsAppliedToNewTasks()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice")
            {
                DefaultTimeToLive = TimeSpan.FromMinutes(5)
            };

            var created = await store.CreateTaskAsync();

            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
        }

        [TestMethod]
        public async Task DefaultTimeToLive_NonPositive_IsUnlimited()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice")
            {
                DefaultTimeToLive = TimeSpan.Zero
            };

            var created = await store.CreateTaskAsync();

            created.TimeToLive.Should().BeNull();
        }

        [TestMethod]
        public async Task GetTaskAsync_AfterOneTtlWhileWorking_StillReturnsTask()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddSeconds(10);
            var fetched = await clock.GetTaskAsync(created.TaskId);

            fetched.Should().NotBeNull();
            fetched!.Status.Should().Be(McpTaskStatus.Working);
            clock.Entries.ContainsKey(created.TaskId).Should().BeTrue();
        }

        [TestMethod]
        public async Task ExpiredTask_IsRemovedOnGet()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(1));
            var created = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddSeconds(2);
            var fetched = await clock.GetTaskAsync(created.TaskId);

            fetched.Should().BeNull();
            clock.Entries.ContainsKey(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task ExpiredTask_IsRemovedOnSweep()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(1), TimeSpan.Zero);
            var created = await clock.CreateTaskAsync();
            clock.Now = clock.Now.AddSeconds(2);

            var survivor = await clock.CreateTaskAsync();

            clock.Entries.ContainsKey(created.TaskId).Should().BeFalse();
            clock.Entries.ContainsKey(survivor.TaskId).Should().BeTrue();
        }

        [TestMethod]
        public async Task CreateTaskAsync_InsertsBeforeSweep()
        {
            var clock = ClockStore("alice", null, TimeSpan.Zero);
            string? idDuringSweep = null;
            clock.OnSweep = () => idDuringSweep = clock.Entries.Keys.FirstOrDefault();

            var created = await clock.CreateTaskAsync();

            idDuringSweep.Should().Be(created.TaskId);
        }

        [TestMethod]
        public async Task Sweep_ThrottlesWhenIntervalHasNotElapsed()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
            var created = await clock.CreateTaskAsync();
            clock.Now = clock.Now.AddSeconds(2);

            await clock.CreateTaskAsync();

            clock.Entries.ContainsKey(created.TaskId).Should().BeTrue();

            clock.Now = clock.Now.Add(clock.Sweep);
            clock.SweepNow();
            clock.Entries.ContainsKey(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task Sweep_DoesNotRemoveSecondWindowWorkingTask()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(20), TimeSpan.Zero);
            var created = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddSeconds(25);
            await clock.CreateTaskAsync();

            clock.Entries.ContainsKey(created.TaskId).Should().BeTrue();
            (await clock.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public async Task Sweep_RemovesTerminalPastTwoWindows()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10), TimeSpan.Zero);
            var created = await clock.CreateTaskAsync();
            await clock.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            clock.Now = clock.Now.AddSeconds(20);
            clock.SweepNow();

            clock.Entries.ContainsKey(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task Sweep_DoesNotRemoveTerminalInsideTwoWindows()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(60), TimeSpan.Zero);
            var created = await clock.CreateTaskAsync();
            await clock.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            clock.Now = clock.Now.AddSeconds(30);
            clock.SweepNow();

            clock.Entries.ContainsKey(created.TaskId).Should().BeTrue();
        }

        [TestMethod]
        public async Task GetTaskAsync_PulsesSweep_RemovesUnpolledExpiredTask()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10), TimeSpan.Zero);
            var victim = await clock.CreateTaskAsync();
            clock.DefaultTimeToLive = null;
            var live = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddSeconds(20);
            (await clock.GetTaskAsync(live.TaskId)).Should().NotBeNull();

            clock.Entries.ContainsKey(victim.TaskId).Should().BeFalse();
            clock.Entries.ContainsKey(live.TaskId).Should().BeTrue();
        }

        [TestMethod]
        public async Task GetTaskAsync_TerminalSnapshot_ServeAndDrop()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var first = await store.GetTaskAsync(created.TaskId);
            first.Should().NotBeNull();
            first!.Status.Should().Be(McpTaskStatus.Completed);
            first.Result!.Value.GetProperty("ok").GetBoolean().Should().BeTrue();

            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task GetTaskAsync_FailedSnapshot_ServeAndDrop()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();
            await inspect.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }));

            (await inspect.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Failed);
            (await inspect.GetTaskAsync(created.TaskId)).Should().BeNull();
            inspect.Entries.ContainsKey(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task GetTaskAsync_CancelledSnapshot_ServeAndDrop()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();
            (await inspect.SetCancelledAsync(created.TaskId)).Should().BeTrue();

            (await inspect.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Cancelled);
            (await inspect.GetTaskAsync(created.TaskId)).Should().BeNull();
            inspect.Entries.ContainsKey(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task GetTaskAsync_InputRequired_DoesNotDrop()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });

            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
            var second = await store.GetTaskAsync(created.TaskId);
            second.Should().NotBeNull();
            second!.Status.Should().Be(McpTaskStatus.InputRequired);
        }

        [TestMethod]
        public async Task GetTaskAsync_OtherUser_DoesNotServeAndDropOwnerRow()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();
            await inspect.SetCompletedAsync(created.TaskId, JsonPayload(new { secret = "alice" }));

            (await inspect.GetTaskAsync(created.TaskId, TestUser.Authenticated("bob"))).Should().BeNull();
            inspect.Entries.ContainsKey(created.TaskId).Should().BeTrue();

            var owner = await inspect.GetTaskAsync(created.TaskId);
            owner.Should().NotBeNull();
            owner!.Status.Should().Be(McpTaskStatus.Completed);
            owner.Result!.Value.GetProperty("secret").GetString().Should().Be("alice");
        }

        [TestMethod]
        public async Task GetTaskAsync_ExpiredTerminal_ReturnsNullWithoutServing()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();
            await clock.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            clock.Now = clock.Now.AddSeconds(20);
            (await clock.GetTaskAsync(created.TaskId)).Should().BeNull();
            clock.Entries.ContainsKey(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task GetTaskAsync_TerminalInsideTwoWindows_IsServed()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();
            clock.Now = clock.Now.AddSeconds(5);
            await clock.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));
            clock.Now = clock.Now.AddSeconds(9);

            var fetched = await clock.GetTaskAsync(created.TaskId);
            fetched.Should().NotBeNull();
            fetched!.Status.Should().Be(McpTaskStatus.Completed);
            fetched.Result!.Value.GetProperty("ok").GetBoolean().Should().BeTrue();
        }

        [TestMethod]
        public async Task SetCompletedAsync_InSecondWindow_IsAllowed()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddSeconds(11);
            await clock.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var fetched = await clock.GetTaskAsync(created.TaskId);
            fetched.Should().NotBeNull();
            fetched!.Status.Should().Be(McpTaskStatus.Completed);
        }

        [TestMethod]
        public async Task SetFailedAsync_InSecondWindow_IsAllowed()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddSeconds(11);
            await clock.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }));

            (await clock.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Failed);
        }

        [TestMethod]
        public async Task SetCancelledAsync_InSecondWindow_IsAllowed()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddSeconds(11);
            (await clock.SetCancelledAsync(created.TaskId)).Should().BeTrue();
            (await clock.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Cancelled);
        }

        [TestMethod]
        public async Task SetInputRequestsAsync_InSecondWindow_IsRefused()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddSeconds(11);
            await clock.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });

            var fetched = await clock.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Working);
            fetched.InputRequests.Should().BeEmpty();
        }

        [TestMethod]
        public async Task ResolveInputRequestsAsync_InSecondWindow_IsRefusedAndDoesNotFireEvent()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();
            await clock.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });

            clock.Now = clock.Now.AddSeconds(11);
            var raised = 0;
            clock.InputResponseReceived += _ => raised++;

            await clock.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            raised.Should().Be(0);
            var fetched = await clock.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.InputRequired);
            fetched.InputRequests.Should().ContainKey("r1");
        }

        [TestMethod]
        public async Task Mutations_AfterHasExpired_AreNoOps()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();
            clock.Now = clock.Now.AddSeconds(20);

            await clock.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));
            await clock.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }));
            (await clock.SetCancelledAsync(created.TaskId)).Should().BeFalse();
            await clock.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });

            var raised = 0;
            clock.InputResponseReceived += _ => raised++;
            await clock.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            raised.Should().Be(0);
            (await clock.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task TryApply_OnExpiredTask_ReturnsMissing()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(1));
            var created = await clock.CreateTaskAsync();
            clock.Now = clock.Now.AddSeconds(5);

            var result = clock.Apply(created.TaskId, current => current, out var applied);

            result.Should().Be(ApplyResult.Missing);
            applied.Should().BeNull();
            clock.Entries.ContainsKey(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task TryApply_WhenCancelled_ThrowsWithoutMutating()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Action act = () => inspect.Apply(created.TaskId, current =>
            {
                var next = current.Clone();
                next.TryCancel(inspect.Now);
                return next;
            }, out _, cts.Token);

            act.Should().Throw<OperationCanceledException>();
            inspect.Entries[created.TaskId].Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public async Task SweepNow_WhenCancelled_ThrowsDuringWalk()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(1), TimeSpan.Zero);
            await clock.CreateTaskAsync();
            clock.Now = clock.Now.AddSeconds(2);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Action act = () => clock.SweepNow(cts.Token);

            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public async Task SweepExpired_ConcurrentCallers_CompareExchangeLoserReturns()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
            await clock.CreateTaskAsync();

            var threadCount = Math.Max(8, Environment.ProcessorCount);
            for (var round = 0; round < 10; round++)
            {
                clock.SweepTicks = clock.Now.UtcTicks - clock.Sweep.Ticks - 1;
                var barrier = new Barrier(threadCount);
                var pulses = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    clock.PulseNow();
                }));

                await Task.WhenAll(pulses);
            }
        }

        [TestMethod]
        public async Task ConcurrentTerminalGets_AtLeastOneServesThenRowIsGone()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();
            await inspect.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => inspect.GetTaskAsync(created.TaskId)));

            results.Count(r => r is not null).Should().BeGreaterThanOrEqualTo(1);
            results.Where(r => r is not null).Should().OnlyContain(r => r!.Status == McpTaskStatus.Completed);
            inspect.Entries.ContainsKey(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task TryApply_RetriesWhenCompareAndSwapLoses()
        {
            var store = new ContendedStore();
            var created = await store.CreateTaskAsync();

            await store.SetCancelledAsync(created.TaskId);

            store.FailedSwaps.Should().Be(1);
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Cancelled);
        }

        [TestMethod]
        public async Task TryApply_WhenUpdaterReturnsNull_IsUnchanged()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();

            var result = inspect.Apply(created.TaskId, _ => null, out var applied);

            result.Should().Be(ApplyResult.Unchanged);
            applied!.Info.TaskId.Should().Be(created.TaskId);
        }

        [TestMethod]
        public async Task NewTaskId_Collision_RetriesUntilUnique()
        {
            var inspect = new InspectableStore("alice");
            var first = await inspect.CreateTaskAsync();
            inspect.ForcedIds.Enqueue(first.TaskId);

            var second = await inspect.CreateTaskAsync();

            second.TaskId.Should().NotBe(first.TaskId);
            inspect.Entries.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task Clone_PreservesUnspecifiedFields()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();

            var cloned = inspect.ClonePublic(created);

            cloned.TaskId.Should().Be(created.TaskId);
            cloned.Status.Should().Be(created.Status);
            cloned.CreatedAt.Should().Be(created.CreatedAt);
            cloned.LastUpdatedAt.Should().Be(created.LastUpdatedAt);
            cloned.PollIntervalMs.Should().Be(created.PollIntervalMs);
        }

        [TestMethod]
        public async Task Clone_OverridesSpecifiedFields()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();
            var payload = JsonPayload(new { ok = true });
            var error = JsonPayload(new { e = 1 });
            var requests = new Dictionary<string, InputRequest> { ["r"] = CreateElicitRequest() };
            var updated = created.CreatedAt.AddMinutes(1);

            var cloned = inspect.ClonePublic(created, McpTaskStatus.Failed, updated, "msg", payload, error, requests);

            cloned.Status.Should().Be(McpTaskStatus.Failed);
            cloned.LastUpdatedAt.Should().Be(updated);
            cloned.StatusMessage.Should().Be("msg");
            cloned.Result.Should().NotBeNull();
            cloned.Error.Should().NotBeNull();
            cloned.InputRequests.Should().ContainKey("r");
        }

        [TestMethod]
        public void NormalizeTimeToLive_Negative_IsUnlimited()
        {
            var inspect = new InspectableStore("alice");

            inspect.Normalize(TimeSpan.FromSeconds(-1)).Should().BeNull();
            inspect.Normalize(null).Should().BeNull();
            inspect.Normalize(TimeSpan.FromSeconds(3)).Should().Be(TimeSpan.FromSeconds(3));
        }

        [TestMethod]
        public void StoredMcpTask_WhenOwnerOrInfoMissing_Throws()
        {
            var info = new McpTaskInfo("id", McpTaskStatus.Working, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, 1000, null, null, null, new Dictionary<string, InputRequest>());

            Action owner = () => _ = new StoredMcpTask(" ", info);
            Action missing = () => _ = new StoredMcpTask("alice", null!);

            owner.Should().Throw<ArgumentException>();
            missing.Should().Throw<ArgumentNullException>().WithParameterName("info");
        }

        [TestMethod]
        public async Task StoredMcpTask_With_ReplacesInfo()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();
            var stored = inspect.Entries[created.TaskId];
            var nextInfo = inspect.ClonePublic(created, status: McpTaskStatus.Cancelled);

            var next = stored.With(nextInfo);

            next.OwnerUserId.Should().Be("alice");
            next.Info.Status.Should().Be(McpTaskStatus.Cancelled);
            next.Should().NotBeSameAs(stored);
        }

        [TestMethod]
        public async Task IsExpired_UnlimitedTtl_IsFalse()
        {
            var inspect = new InspectableStore("alice");
            var created = await inspect.CreateTaskAsync();
            var stored = inspect.Entries[created.TaskId];

            inspect.Expired(stored, inspect.Now.AddYears(1)).Should().BeFalse();
            inspect.HasExpired(stored, inspect.Now.AddYears(1)).Should().BeFalse();
            inspect.Active(stored, inspect.Now.AddYears(1)).Should().BeTrue();
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenCancelled_Throws()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await store.CreateTaskAsync(cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenCancelled_Throws()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await store.GetTaskAsync("id", cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task PublicMethods_CanceledToken_ThrowWithoutMutating()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var get = async () => await store.GetTaskAsync(created.TaskId, cts.Token);
            var complete = async () => await store.SetCompletedAsync(created.TaskId, JsonPayload(true), cts.Token);
            var fail = async () => await store.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }), cts.Token);
            var cancel = async () => await store.SetCancelledAsync(created.TaskId, cts.Token);
            var input = async () => await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            }, cts.Token);
            var resolve = async () => await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            }, cts.Token);

            await get.Should().ThrowAsync<OperationCanceledException>();
            await complete.Should().ThrowAsync<OperationCanceledException>();
            await fail.Should().ThrowAsync<OperationCanceledException>();
            await cancel.Should().ThrowAsync<OperationCanceledException>();
            await input.Should().ThrowAsync<OperationCanceledException>();
            await resolve.Should().ThrowAsync<OperationCanceledException>();

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public async Task GetTaskAsync_WithNegativeTimeToLive_DoesNotExpire()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(-5));
            var created = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddDays(1);

            var fetched = await clock.GetTaskAsync(created.TaskId);
            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
        }

        [TestMethod]
        public async Task DefaultTimeToLive_ChangeAfterCreate_DoesNotAffectExistingTask()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(10));
            var created = await clock.CreateTaskAsync();

            clock.DefaultTimeToLive = TimeSpan.FromMilliseconds(1);
            clock.Now = clock.Now.AddSeconds(5);

            var fetched = await clock.GetTaskAsync(created.TaskId);
            fetched.Should().NotBeNull();
            fetched!.TimeToLive.Should().Be(TimeSpan.FromSeconds(10));
        }

        [TestMethod]
        public async Task SetCompletedAsync_UpdatesLastUpdatedAtFromClock()
        {
            var clock = ClockStore("alice", null);
            var created = await clock.CreateTaskAsync();
            var start = clock.Now;

            clock.Now = clock.Now.AddSeconds(3);
            await clock.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var fetched = await clock.GetTaskAsync(created.TaskId);
            fetched!.CreatedAt.Should().Be(start);
            fetched.LastUpdatedAt.Should().Be(start.AddSeconds(3));
        }

        [TestMethod]
        public async Task AutoSweepOff_CreateDoesNotFlush_HandleStillDoes()
        {
            var clock = ClockStore("alice", TimeSpan.FromSeconds(1), TimeSpan.Zero);
            clock.AutoSweep = false;
            var victim = await clock.CreateTaskAsync();

            clock.Now = clock.Now.AddSeconds(2);
            await clock.CreateTaskAsync();

            clock.Entries.ContainsKey(victim.TaskId).Should().BeTrue();

            await clock.SweepExpiredAsync();
            clock.Entries.ContainsKey(victim.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task ServeAndDropOff_OwnerGetKeepsTerminalRow()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice") { ServeAndDrop = false };
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var first = await store.GetTaskAsync(created.TaskId);
            var second = await store.GetTaskAsync(created.TaskId);

            first!.Status.Should().Be(McpTaskStatus.Completed);
            second!.Status.Should().Be(McpTaskStatus.Completed);
        }

        [TestMethod]
        public async Task ShouldDiscardOverride_IsUsedByFlush()
        {
            var store = new AlwaysDiscardStore();
            store.AutoSweep = false;
            var created = await store.CreateTaskAsync();

            store.Entries.ContainsKey(created.TaskId).Should().BeTrue();
            store.SweepNow();
            store.Entries.ContainsKey(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task ConcurrentCreates_ProduceUniqueIds()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var tasks = Enumerable.Range(0, 32).Select(_ => store.CreateTaskAsync()).ToArray();

            var created = await Task.WhenAll(tasks);

            created.Select(t => t.TaskId).Distinct().Should().HaveCount(32);
        }

        [TestMethod]
        public async Task ConcurrentSetInputRequests_DoNotLoseKeys()
        {
            var store = new InMemoryAuthenticatedMcpTaskStore("alice");
            var created = await store.CreateTaskAsync();
            var writes = Enumerable.Range(0, 20).Select(i =>
                store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
                {
                    [$"r{i}"] = CreateElicitRequest()
                }));

            await Task.WhenAll(writes);

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched!.InputRequests.Should().HaveCount(20);
        }

        [TestMethod]
        public async Task IsolationOverride_CanAllowCrossUserRead()
        {
            var store = new OpenIsolationStore();
            var created = await store.CreateTaskAsync(TestUser.Authenticated("alice"));

            var fetched = await store.GetTaskAsync(created.TaskId, TestUser.Authenticated("bob"));

            fetched.Should().NotBeNull();
        }

        #endregion

        #region Private Methods

        private static InspectableStore ClockStore(string userId, TimeSpan? ttl, TimeSpan? sweep = null) =>
            new(userId)
            {
                Now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Sweep = sweep ?? TimeSpan.FromHours(1),
                DefaultTimeToLive = ttl
            };

        private static JsonElement JsonPayload<T>(T value) => JsonSerializer.SerializeToElement(value);

        private static InputRequest CreateElicitRequest() =>
            InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" });

        private static InputResponse CreateElicitResponse() =>
            InputResponse.FromElicitResult(new ElicitResult { Action = "accept" });

        #endregion

        #region Nested Types

        private class AlwaysDiscardStore : InspectableStore
        {

            public AlwaysDiscardStore()
                : base("alice")
            {
            }

            protected override bool ShouldDiscard(StoredMcpTask task, DateTimeOffset now) => true;

        }

        #endregion

    }

}
