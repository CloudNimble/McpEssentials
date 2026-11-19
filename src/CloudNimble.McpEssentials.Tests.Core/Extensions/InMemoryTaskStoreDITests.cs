using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Tests.Core.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using System;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Extensions
{

    /// <summary>
    /// Breakdance host coverage for <c>AddInMemoryMcpTaskStorePlus</c>.
    /// </summary>
    [TestClass]
    public class InMemoryTaskStoreDITests : BreakdanceMSTestBase
    {

        #region Fields

        private string _userId = "alice";

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            _userId = "alice";
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddInMemoryMcpTaskStorePlus(() => _userId);
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
                services.AddInMemoryMcpTaskStorePlus(_ => TestUser.Authenticated("principal-1"));
            });

            var store = GetService<IMcpTaskStore>();
            store.Should().BeSameAs(GetService<IMcpTaskStorePlus>());
            var created = await store!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task ResolvesTheSameSingleton()
        {
            RebuildHost(services => services.AddInMemoryMcpTaskStorePlus("alice"));

            var store1 = GetService<IMcpTaskStore>();
            var store2 = GetService<IMcpTaskStorePlus>();
            var concrete = GetService<InMemoryMcpTaskStorePlus>();

            store1.Should().BeSameAs(store2);
            store1.Should().BeSameAs(concrete);
            store1.Should().BeOfType<InMemoryMcpTaskStorePlus>();

            var created = await store1!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task StoreOptions_ApplyToCreatedTasks()
        {
            RebuildHost(services =>
            {
                services.AddInMemoryMcpTaskStorePlus("alice", o =>
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
