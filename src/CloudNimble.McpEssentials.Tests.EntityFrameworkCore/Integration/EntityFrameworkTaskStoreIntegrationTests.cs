using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Integration
{

    /// <summary>
    /// Breakdance host tests that resolve the EF store from DI and prove per-user isolation
    /// through both <see cref="IMcpTaskStore"/> and <see cref="IPerUserMcpTaskStore"/> against SQLite.
    /// </summary>
    [TestClass]
    public class EntityFrameworkTaskStoreIntegrationTests : BreakdanceMSTestBase
    {

        #region Fields

        private TestUser _user = null!;
        private SqliteConnection _connection = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            _user = new TestUser("alice");
            _connection = new SqliteConnection($"Data Source=file:mcp-int-{Guid.NewGuid():N}?mode=memory&cache=shared");
            _connection.Open();
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_connection);
                services.AddDbContext<McpTaskDbContext>(o => o.UseSqlite(_connection));
                services.AddMcpEssentialsEntityFrameworkTasks<McpTaskDbContext>(() => _user.UserId);
            });
            TestSetup();
            using var scope = GetService<IServiceScopeFactory>()!.CreateScope();
            scope.ServiceProvider.GetRequiredService<McpTaskDbContext>().Database.EnsureCreated();
        }

        [TestCleanup]
        public void TearDown()
        {
            TestTearDown();
            _connection.Dispose();
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
        public async Task Host_IPerUserMcpTaskStore_UsesExplicitPrincipals()
        {
            var store = GetService<IPerUserMcpTaskStore>();
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
            var perUser = GetService<IPerUserMcpTaskStore>();
            var concrete = GetService<EntityFrameworkAuthenticatedTaskStore<McpTaskDbContext>>();

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
            var store = GetService<IPerUserMcpTaskStore>();
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
        public async Task Host_NewScope_SeesPersistedTask()
        {
            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            using var scope = GetService<IServiceScopeFactory>()!.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<McpTaskDbContext>();
            var row = await context.Set<McpTaskEntity>().SingleAsync(e => e.TaskId == created.TaskId);

            row.OwnerUserId.Should().Be("alice");
            row.Status.Should().Be(McpTaskStatus.Working);
        }

        #endregion

    }

}
