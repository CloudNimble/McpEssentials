using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Redis.Tasks;
using CloudNimble.McpEssentials.Tests.Redis.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Redis.Integration
{

    /// <summary>
    /// Breakdance host tests that resolve the Redis store from DI and prove per-user isolation
    /// through both <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/> against Garnet.
    /// </summary>
    [TestClass]
    public class RedisTaskStoreIntegrationTests : BreakdanceMSTestBase
    {

        #region Fields

        private TestUser _user = null!;
        private RedisStoreTestContext _redis = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            _user = new TestUser("alice");
            _redis = new RedisStoreTestContext();
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IConnectionMultiplexer>(_redis.Multiplexer);
                services.AddMcpEssentialsRedisTasks(() => _user.UserId);
            });
            TestSetup();
        }

        [TestCleanup]
        public void TearDown()
        {
            TestTearDown();
            _redis?.Dispose();
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public async Task Host_IMcpTaskStore_IsolatesUsers()
        {
            var store = GetService<IMcpTaskStore>();
            store.Should().NotBeNull();

            var aliceTask = await store!.CreateTaskAsync();
            await store.SetCompletedAsync(aliceTask.TaskId, JsonSerializer.SerializeToElement(new { secret = "alice" }));

            _user.UserId = "bob";
            (await store.GetTaskAsync(aliceTask.TaskId)).Should().BeNull();
            (await store.SetCancelledAsync(aliceTask.TaskId)).Should().BeFalse();

            var bobTask = await store.CreateTaskAsync();
            _user.UserId = "alice";
            (await store.GetTaskAsync(bobTask.TaskId)).Should().BeNull();

            var aliceFetched = await store.GetTaskAsync(aliceTask.TaskId);
            aliceFetched.Should().NotBeNull();
            aliceFetched!.Result!.Value.GetProperty("secret").GetString().Should().Be("alice");
        }

        [TestMethod]
        public async Task Host_IMcpTaskStorePlus_UsesExplicitPrincipals()
        {
            var store = GetService<IMcpTaskStorePlus>();
            var alice = TestUser.Authenticated("alice");
            var bob = TestUser.Authenticated("bob");

            var created = await store!.CreateTaskAsync(alice);
            (await store.GetTaskAsync(created.TaskId, bob)).Should().BeNull();
            (await store.GetTaskAsync(created.TaskId, alice)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task Host_SameInstance_ForAllRegistrations()
        {
            var mcp = GetService<IMcpTaskStore>();
            var perUser = GetService<IMcpTaskStorePlus>();
            var concrete = GetService<RedisMcpTaskStore>();

            mcp.Should().BeSameAs(perUser);
            mcp.Should().BeSameAs(concrete);

            var created = await perUser!.CreateTaskAsync(TestUser.Authenticated("alice"));
            (await mcp!.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task Host_BackgroundCompletion_SkipsLiveUser()
        {
            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            _user.UserId = null;
            await store.SetFailedAsync(created.TaskId, JsonSerializer.SerializeToElement(new { message = "boom" }));

            _user.UserId = "alice";
            (await store.GetTaskAsync(created.TaskId))!.Status.Should().Be(McpTaskStatus.Failed);
        }

        [TestMethod]
        public async Task Host_ResolveInput_OnlyOwnerRaisesEvent()
        {
            var store = GetService<IMcpTaskStorePlus>();
            var created = await store!.CreateTaskAsync(TestUser.Authenticated("alice"));
            var raised = 0;
            store.InputResponseReceived += _ => raised++;

            await store.SetInputRequestsAsync(created.TaskId, new Dictionary<string, InputRequest>
            {
                ["r1"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" })
            });

            await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = InputResponse.FromElicitResult(new ElicitResult { Action = "accept" })
            }, TestUser.Authenticated("bob"));
            raised.Should().Be(0);

            await store.ResolveInputRequestsAsync(created.TaskId, new Dictionary<string, InputResponse>
            {
                ["r1"] = InputResponse.FromElicitResult(new ElicitResult { Action = "accept" })
            }, TestUser.Authenticated("alice"));
            raised.Should().Be(1);
        }

        [TestMethod]
        public async Task Host_NewConnection_SeesPersistedTask()
        {
            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            _redis.KeyExists(created.TaskId).Should().BeTrue();
            var json = (string?)_redis.Database.StringGet($"mcp:task:{created.TaskId}");
            json.Should().Contain("alice");
        }

        #endregion

    }

}
