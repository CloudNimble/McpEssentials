using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Redis.Tasks;
using CloudNimble.McpEssentials.Tests.Redis.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using StackExchange.Redis;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Redis.Extensions
{

    /// <summary>
    /// Breakdance coverage for Redis store DI registration and WithRedisTasks.
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsTests : BreakdanceMSTestBase
    {

        #region Fields

        private RedisStoreTestContext? _redis;
        private string _userId = "alice";

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            _userId = "alice";
            ConfigureRedisHost(services => services.AddMcpEssentialsRedisTasks(() => _userId));
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
        public async Task AddMcpEssentialsRedisTasks_ConstantUser_ResolvesSingletonStore()
        {
            var store1 = GetService<IMcpTaskStore>();
            var store2 = GetService<IMcpTaskStorePlus>();
            var concrete = GetService<RedisMcpTaskStore>();

            store1.Should().BeSameAs(store2);
            store1.Should().BeSameAs(concrete);

            var created = await store1!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task AddMcpEssentialsRedisTasks_FromUserIdFactory_UsesCallback()
        {
            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            _userId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task AddMcpEssentialsRedisTasks_FromPrincipalFactory_UsesClaims()
        {
            ConfigureRedisHost(services =>
            {
                services.AddMcpEssentialsRedisTasks(_ => TestUser.Authenticated("principal-1"));
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void AddMcpEssentialsRedisTasks_WhenServicesIsNull_Throws()
        {
            IServiceCollection services = null!;

            Action act = () => services.AddMcpEssentialsRedisTasks("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [TestMethod]
        public void AddMcpEssentialsRedisTasks_WhenUserIdIsWhitespace_Throws()
        {
            var services = new ServiceCollection();

            Action act = () => services.AddMcpEssentialsRedisTasks("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public async Task AddMcpEssentialsRedisTasks_Configure_SetsDefaultTimeToLiveAndPrefix()
        {
            ConfigureRedisHost(services =>
            {
                services.AddMcpEssentialsRedisTasks("alice", o =>
                {
                    o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                    o.DefaultPollIntervalMs = 2500;
                    o.KeyPrefix = "app";
                });
            });

            var store = GetService<RedisMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            store.KeyPrefix.Should().Be("app");
            store.AutoSweep.Should().BeFalse();
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
            _redis!.KeyExists(created.TaskId, "app").Should().BeTrue();
        }

        [TestMethod]
        public async Task WithRedisTasks_Configure_SetsDefaultTimeToLive()
        {
            ConfigureRedisHost(services =>
            {
                services.AddMcpServer().WithRedisTasks("alice", o =>
                {
                    o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                    o.DefaultPollIntervalMs = 2500;
                });
            });

            var store = GetService<RedisMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public async Task WithRedisTasks_ConstantUser_CreatesIsolatedStore()
        {
            ConfigureRedisHost(services =>
            {
                services.AddMcpServer().WithRedisTasks("alice");
            });

            var store = GetService<IMcpTaskStore>();
            var perUser = GetService<IMcpTaskStorePlus>();
            var created = await store!.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeOfType<RedisMcpTaskStore>();
            store.Should().BeSameAs(perUser);
        }

        [TestMethod]
        public async Task WithRedisTasks_FromServiceProvider_ResolvesRegisteredPrincipal()
        {
            ConfigureRedisHost(services =>
            {
                services.AddSingleton(TestUser.Authenticated("alice"));
                services.AddMcpServer().WithRedisTasks(sp => sp.GetRequiredService<ClaimsPrincipal>());
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public void WithRedisTasks_FromServiceProvider_CapturesProviderWhenOptionsAreConfigured()
        {
            var configured = false;
            ConfigureRedisHost(services =>
            {
                services.AddSingleton(TestUser.Authenticated("alice"));
                services.AddMcpServer().WithRedisTasks(sp => sp.GetRequiredService<ClaimsPrincipal>(), configureTasks: _ => configured = true);
            });

            configured.Should().BeTrue();
        }

        [TestMethod]
        public void WithRedisTasks_WhenUserIdIsWhitespace_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithRedisTasks("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void WithRedisTasks_WhenBuilderIsNull_Throws()
        {
            IMcpServerBuilder builder = null!;

            Action act = () => builder.WithRedisTasks("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        [TestMethod]
        public void WithRedisTasks_WhenPrincipalFactoryIsNull_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithRedisTasks((Func<IServiceProvider, ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public async Task WithRedisTasks_FromUserIdFactory_CreatesTask()
        {
            ConfigureRedisHost(services =>
            {
                services.AddMcpServer().WithRedisTasks(() => "alice");
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task WithRedisTasks_FromPrincipalFactory_CreatesTask()
        {
            ConfigureRedisHost(services =>
            {
                services.AddMcpServer().WithRedisTasks(() => TestUser.Authenticated("alice"));
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        #endregion

        #region Private Methods

        private void ConfigureRedisHost(Action<IServiceCollection> configure)
        {
            ResetTestHostBuilder();
            _redis?.Dispose();
            _redis = new RedisStoreTestContext();
            var multiplexer = _redis.Multiplexer;
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IConnectionMultiplexer>(multiplexer);
                configure(services);
            });
            TestSetup();
        }

        #endregion

    }

}
