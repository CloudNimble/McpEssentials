using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Tests.Core.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Extensions
{

    /// <summary>
    /// Breakdance host coverage for <c>WithTasksPlus</c>.
    /// </summary>
    [TestClass]
    public class WithTasksPlusDITests : BreakdanceMSTestBase
    {

        #region Fields

        private bool _tasksConfigured;
        private string _userId = "alice";

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            _userId = "alice";
            _tasksConfigured = false;
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddMcpServer().WithTasksPlus(() => _userId, configureTasks: _ => _tasksConfigured = true);
            });
            TestSetup();
        }

        [TestCleanup]
        public void TearDown()
        {
            TestTearDown();
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public void ConfigureTasks_RunsWhenTheHostStarts()
        {
            _tasksConfigured.Should().BeTrue();
        }

        [TestMethod]
        public async Task ConstantUser_CreatesAnIsolatedStore()
        {
            RebuildHost(services => services.AddMcpServer().WithTasksPlus("alice"));

            var store = GetService<IMcpTaskStore>();
            var perUser = GetService<IMcpTaskStorePlus>();
            var created = await store!.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeOfType<InMemoryMcpTaskStorePlus>();
            store.Should().BeSameAs(perUser);
        }

        [TestMethod]
        public async Task Factory_IsolatesWhenTheUserChanges()
        {
            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            _userId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task FromPrincipal_CreatesATask()
        {
            RebuildHost(services =>
            {
                services.AddMcpServer().WithTasksPlus(() => TestUser.Authenticated("principal-1"));
            });

            var created = await GetService<IMcpTaskStore>()!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task FromServiceProvider_IsolatesWhenTheResolvedUserChanges()
        {
            var current = new TestUser("alice");
            RebuildHost(services =>
            {
                services.AddSingleton(current);
                services.AddMcpServer().WithTasksPlus(sp => TestUser.Authenticated(sp.GetRequiredService<TestUser>().UserId!));
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            current.UserId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task FromServiceProvider_ResolvesTheRegisteredPrincipal()
        {
            RebuildHost(services =>
            {
                services.AddSingleton(TestUser.Authenticated("alice"));
                services.AddMcpServer().WithTasksPlus(sp => sp.GetRequiredService<ClaimsPrincipal>());
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            store.Should().BeSameAs(GetService<IMcpTaskStorePlus>());
            store.Should().BeSameAs(GetService<InMemoryMcpTaskStorePlus>());
            created.TaskId.Should().NotBeNullOrWhiteSpace();
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task StoreOptions_ApplyToCreatedTasks()
        {
            RebuildHost(services =>
            {
                services.AddMcpServer().WithTasksPlus("alice", o =>
                {
                    o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                    o.DefaultPollIntervalMs = 2500;
                });
            });

            var store = GetService<InMemoryMcpTaskStorePlus>();
            var created = await store!.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public async Task UseInMemory_CreatesAnIsolatedStore()
        {
            RebuildHost(services =>
            {
                services.AddMcpServer().WithTasksPlus(tasks => tasks.UseInMemory("alice"));
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeOfType<InMemoryMcpTaskStorePlus>();
        }

        [TestMethod]
        public async Task UseInMemory_FromServiceProvider_ResolvesTheUser()
        {
            RebuildHost(services =>
            {
                services.AddSingleton("alice");
                services.AddMcpServer().WithTasksPlus(tasks => tasks.UseInMemory(sp => sp.GetRequiredService<string>()));
            });

            var created = await GetService<IMcpTaskStore>()!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Rebuilds the Breakdance host with a different service graph.
        /// </summary>
        /// <param name="configure">The replacement service configuration.</param>
        internal void RebuildHost(Action<IServiceCollection> configure)
        {
            ResetTestHostBuilder();
            TestHostBuilder.ConfigureServices(configure);
            TestSetup();
        }

        #endregion

    }

}
