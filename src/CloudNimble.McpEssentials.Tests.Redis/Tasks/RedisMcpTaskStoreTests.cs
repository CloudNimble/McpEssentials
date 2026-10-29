using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Redis.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Redis.Tasks
{

    /// <summary>
    /// Garnet-backed coverage for the Redis authenticated task store.
    /// </summary>
    [TestClass]
    public class RedisMcpTaskStoreTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public async Task CreateTaskAsync_RecordsCurrentUser_AndOwnerCanGet()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");

            var created = await store.CreateTaskAsync();
            var fetched = await store.GetTaskAsync(created.TaskId);

            created.TaskId.Should().HaveLength(32);
            created.Status.Should().Be(McpTaskStatus.Working);
            created.PollIntervalMs.Should().Be(1000);
            created.TimeToLive.Should().BeNull();
            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
            redis.KeyExists(created.TaskId).Should().BeTrue();
        }

        [TestMethod]
        public async Task CreateTaskAsync_IPerUser_UsesPrincipal()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore((string?)null);
            var alice = TestUser.Authenticated("alice");

            var created = await store.CreateTaskAsync(alice);
            var fetched = await store.GetTaskAsync(created.TaskId, alice);

            fetched.Should().NotBeNull();
            fetched!.TaskId.Should().Be(created.TaskId);
        }

        [TestMethod]
        public async Task PrincipalCtor_UsesClaims()
        {
            using var redis = new RedisStoreTestContext();
            ClaimsPrincipal? current = TestUser.Authenticated("alice");
            var store = new RedisMcpTaskStore(redis.Multiplexer, () => current);

            var created = await store.CreateTaskAsync();
            current = TestUser.Authenticated("bob");

            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();

            current = TestUser.Authenticated("alice");
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task CreateTaskAsync_WhenUnauthenticated_Throws()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore((string?)null);

            var act = async () => await store.CreateTaskAsync();

            await act.Should().ThrowAsync<McpTaskUserRequiredException>();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenDifferentUser_ReturnsNull()
        {
            using var redis = new RedisStoreTestContext();
            var user = new TestUser("alice");
            var store = redis.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = "bob";
            var fetched = await store.GetTaskAsync(created.TaskId);

            fetched.Should().BeNull();
            redis.KeyExists(created.TaskId).Should().BeTrue();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenUnknownId_ReturnsNull()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");

            (await store.GetTaskAsync("does-not-exist")).Should().BeNull();
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenOwner_Cancels()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
            var created = await store.CreateTaskAsync();

            var cancelled = await store.SetCancelledAsync(created.TaskId);
            var fetched = await store.GetTaskAsync(created.TaskId);

            cancelled.Should().BeTrue();
            fetched!.Status.Should().Be(McpTaskStatus.Cancelled);
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenDifferentUser_ReturnsFalse_AndLeavesTaskWorking()
        {
            using var redis = new RedisStoreTestContext();
            var user = new TestUser("alice");
            var store = redis.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = "bob";
            (await store.SetCancelledAsync(created.TaskId)).Should().BeFalse();

            user.UserId = "alice";
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Working);
        }

        [TestMethod]
        public async Task SetCancelledAsync_WhenAlreadyTerminal_ReturnsFalse()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            (await store.SetCancelledAsync(created.TaskId)).Should().BeFalse();
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Completed);
        }

        [TestMethod]
        public async Task SetCompletedAsync_WithoutCurrentUser_StillCompletes()
        {
            using var redis = new RedisStoreTestContext();
            var user = new TestUser("alice");
            var store = redis.CreateStore(() => user.UserId);
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
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
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
            using var redis = new RedisStoreTestContext();
            var user = new TestUser("alice");
            var store = redis.CreateStore(() => user.UserId);
            var created = await store.CreateTaskAsync();

            user.UserId = null;
            await store.SetFailedAsync(created.TaskId, JsonPayload(new { message = "boom" }));

            user.UserId = "alice";
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Failed);
        }

        [TestMethod]
        public async Task UsersCannotSeeEachOthersTasks_OrPayloads()
        {
            using var redis = new RedisStoreTestContext();
            var user = new TestUser("alice");
            var store = redis.CreateStore(() => user.UserId);
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
            using var redis = new RedisStoreTestContext();
            var user = new TestUser("alice");
            var store = redis.CreateStore(() => user.UserId);
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
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
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
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
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
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
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
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
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
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
            store.DefaultTimeToLive = TimeSpan.FromMinutes(5);

            var created = await store.CreateTaskAsync();

            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
        }

        [TestMethod]
        public void AutoSweep_DefaultsToFalse()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");

            store.AutoSweep.Should().BeFalse();
        }

        [TestMethod]
        public async Task CreateTaskAsync_UnlimitedTtl_LeavesKeyWithoutExpiry()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");

            var created = await store.CreateTaskAsync();

            redis.KeyTtl(created.TaskId).Should().BeNull();
        }

        [TestMethod]
        public async Task CreateTaskAsync_StampsPexpireAtFromDiscardAfter()
        {
            using var redis = new RedisStoreTestContext();
            var now = DateTimeOffset.UtcNow;
            var store = new ClockRedisStore(redis.Multiplexer, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10)
            };

            var created = await store.CreateTaskAsync();
            var ttl = redis.KeyTtl(created.TaskId);

            ttl.Should().NotBeNull();
            ttl!.Value.TotalSeconds.Should().BeGreaterThan(15);
            ttl.Value.TotalSeconds.Should().BeLessThanOrEqualTo(20);
        }

        [TestMethod]
        public async Task CreateTaskAsync_StampsDiscardAfter()
        {
            using var redis = new RedisStoreTestContext();
            var now = DateTimeOffset.UtcNow;
            var store = new ClockRedisStore(redis.Multiplexer, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10)
            };

            var created = await store.CreateTaskAsync();
            created.TimeToLive.Should().Be(TimeSpan.FromSeconds(10));

            var json = (string?)redis.Database.StringGet($"mcp:task:{created.TaskId}");
            json.Should().NotBeNullOrWhiteSpace();
            var record = JsonSerializer.Deserialize(json!, RedisMcpTasksJsonContext.Default.RedisStoredMcpTask);
            record.Should().NotBeNull();
            record!.ExpiresAt.Should().Be(now.AddSeconds(10));
            record.DiscardAfter.Should().Be(now.AddSeconds(20));
        }

        [TestMethod]
        public async Task Complete_DoesNotMoveDiscardAfter()
        {
            using var redis = new RedisStoreTestContext();
            var now = DateTimeOffset.UtcNow;
            var store = new ClockRedisStore(redis.Multiplexer, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10),
                ServeAndDrop = false
            };

            var created = await store.CreateTaskAsync();
            store.Now = now.AddSeconds(15);
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var json = (string?)redis.Database.StringGet($"mcp:task:{created.TaskId}");
            var record = JsonSerializer.Deserialize(json!, RedisMcpTasksJsonContext.Default.RedisStoredMcpTask);
            record!.DiscardAfter.Should().Be(now.AddSeconds(20));
            record.Status.Should().Be(McpTaskStatus.Completed);
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenClockPastDiscardAfter_DeletesKey()
        {
            using var redis = new RedisStoreTestContext();
            var now = DateTimeOffset.UtcNow;
            var store = new ClockRedisStore(redis.Multiplexer, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10)
            };
            var created = await store.CreateTaskAsync();

            store.Now = now.AddSeconds(20);
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
            redis.KeyExists(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task Complete_InSecondWindow_Succeeds()
        {
            using var redis = new RedisStoreTestContext();
            var now = DateTimeOffset.UtcNow;
            var store = new ClockRedisStore(redis.Multiplexer, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10),
                ServeAndDrop = false
            };
            var created = await store.CreateTaskAsync();

            store.Now = now.AddSeconds(15);
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var fetched = await store.GetTaskAsync(created.TaskId);
            fetched.Should().NotBeNull();
            fetched!.Status.Should().Be(McpTaskStatus.Completed);
        }

        [TestMethod]
        public async Task Mutations_AfterHasExpired_AreNoOps()
        {
            using var redis = new RedisStoreTestContext();
            var now = DateTimeOffset.UtcNow;
            var store = new ClockRedisStore(redis.Multiplexer, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10)
            };
            var created = await store.CreateTaskAsync();

            store.Now = now.AddSeconds(20);
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));
            (await store.SetCancelledAsync(created.TaskId)).Should().BeFalse();
            redis.KeyExists(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task SweepExpired_IsNoOp_KeyRemainsUntilGet()
        {
            using var redis = new RedisStoreTestContext();
            var now = DateTimeOffset.UtcNow;
            var store = new ClockRedisStore(redis.Multiplexer, "alice", now)
            {
                DefaultTimeToLive = TimeSpan.FromSeconds(10)
            };
            var created = await store.CreateTaskAsync();

            store.Now = now.AddSeconds(20);
            await store.SweepExpiredAsync();
            redis.KeyExists(created.TaskId).Should().BeTrue();

            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
            redis.KeyExists(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task GetTaskAsync_TerminalSnapshot_ServeAndDrop()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            var first = await store.GetTaskAsync(created.TaskId);
            first.Should().NotBeNull();
            first!.Status.Should().Be(McpTaskStatus.Completed);
            first.Result!.Value.GetProperty("ok").GetBoolean().Should().BeTrue();

            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
            redis.KeyExists(created.TaskId).Should().BeFalse();
        }

        [TestMethod]
        public async Task GetTaskAsync_OtherUser_DoesNotServeAndDropOwnerRow()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { secret = "alice" }));

            (await store.GetTaskAsync(created.TaskId, TestUser.Authenticated("bob"))).Should().BeNull();
            redis.KeyExists(created.TaskId).Should().BeTrue();

            var owner = await store.GetTaskAsync(created.TaskId);
            owner.Should().NotBeNull();
            owner!.Status.Should().Be(McpTaskStatus.Completed);
            owner.Result!.Value.GetProperty("secret").GetString().Should().Be("alice");
        }

        [TestMethod]
        public async Task ServeAndDropOff_OwnerGetKeepsTerminalRow()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
            store.ServeAndDrop = false;
            var created = await store.CreateTaskAsync();
            await store.SetCompletedAsync(created.TaskId, JsonPayload(new { ok = true }));

            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Completed);
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Completed);
            redis.KeyExists(created.TaskId).Should().BeTrue();
        }

        [TestMethod]
        public async Task GetTaskAsync_InputRequired_DoesNotDrop()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
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
        public async Task KeyPrefix_IsolatesKeysAndChannels()
        {
            using var redis = new RedisStoreTestContext();
            var mcp = redis.CreateStore("alice");
            var app = redis.CreateStore("alice");
            app.KeyPrefix = "app";

            var created = await mcp.CreateTaskAsync();
            redis.KeyExists(created.TaskId).Should().BeTrue();
            redis.KeyExists(created.TaskId, "app").Should().BeFalse();

            (await app.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task PubSub_OtherStoreReceivesResolvedInput_LocalStoreDoesNotEcho()
        {
            using var redis = new RedisStoreTestContext();
            using var publisher = redis.CreateStore("alice");
            using var subscriber = redis.CreateStore("alice");
            var created = await publisher.CreateTaskAsync();
            await publisher.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = CreateElicitRequest()
            });

            var localRaised = 0;
            publisher.InputResponseReceived += _ => localRaised++;
            var remote = new TaskCompletionSource<InputResponseReceivedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            subscriber.InputResponseReceived += args => remote.TrySetResult(args);
            (await subscriber.GetTaskAsync(created.TaskId)).Should().NotBeNull();

            await publisher.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = CreateElicitResponse()
            });

            localRaised.Should().Be(1);
            var received = await remote.Task.WaitAsync(TimeSpan.FromSeconds(5));
            received.TaskId.Should().Be(created.TaskId);
            received.RequestId.Should().Be("r1");
        }

        [TestMethod]
        public async Task ConcurrentCreates_ProduceUniqueIds()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
            var tasks = Enumerable.Range(0, 16).Select(_ => store.CreateTaskAsync()).ToArray();

            var created = await Task.WhenAll(tasks);

            created.Select(t => t.TaskId).Distinct().Should().HaveCount(16);
        }

        [TestMethod]
        public async Task ConcurrentSetInputRequests_DoNotLoseKeys()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
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
        public async Task CreateTaskAsync_WhenCancelled_Throws()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await store.CreateTaskAsync(cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task GetTaskAsync_WhenTaskIdIsWhitespace_Throws()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");

            var act = async () => await store.GetTaskAsync("  ");

            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("taskId");
        }

        [TestMethod]
        public void Constructor_WhenMultiplexerIsNull_Throws()
        {
            Action act = () => _ = new RedisMcpTaskStore((IConnectionMultiplexer)null!, "alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("multiplexer");
        }

        [TestMethod]
        public void Constructor_WhenGetCurrentUserIdIsNull_Throws()
        {
            using var redis = new RedisStoreTestContext();

            Action act = () => _ = new RedisMcpTaskStore(redis.Multiplexer, (Func<string?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public async Task Mapper_RoundTripsResultAndInputRequests()
        {
            using var redis = new RedisStoreTestContext();
            var store = redis.CreateStore("alice");
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
