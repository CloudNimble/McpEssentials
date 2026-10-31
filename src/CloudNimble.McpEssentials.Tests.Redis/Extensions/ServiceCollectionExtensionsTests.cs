using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Redis.Tasks;
using CloudNimble.McpEssentials.Tests.Redis.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Server;
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

        #endregion

        #region Test Lifecycle

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
            ConfigureRedisHost(services =>
            {
                services.AddMcpEssentialsRedisTasks("alice");
            });

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
            var userId = "alice";
            ConfigureRedisHost(services =>
            {
                services.AddMcpEssentialsRedisTasks(() => userId);
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            userId = "bob";
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
            using var redis = new RedisStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton<IConnectionMultiplexer>(redis.Multiplexer);

            builder.WithRedisTasks("alice", o =>
            {
                o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                o.DefaultPollIntervalMs = 2500;
            });

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<RedisMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public async Task WithRedisTasks_ConstantUser_CreatesIsolatedStore()
        {
            using var redis = new RedisStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton<IConnectionMultiplexer>(redis.Multiplexer);

            builder.WithRedisTasks("alice");

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var perUser = provider.GetRequiredService<IMcpTaskStorePlus>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeOfType<RedisMcpTaskStore>();
            store.Should().BeSameAs(perUser);
        }

        [TestMethod]
        public async Task WithRedisTasks_FromServiceProvider_ResolvesRegisteredPrincipal()
        {
            using var redis = new RedisStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton<IConnectionMultiplexer>(redis.Multiplexer);
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));

            builder.WithRedisTasks(sp => sp.GetRequiredService<ClaimsPrincipal>());

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public void WithRedisTasks_FromServiceProvider_CapturesProviderWhenOptionsAreConfigured()
        {
            using var redis = new RedisStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton<IConnectionMultiplexer>(redis.Multiplexer);
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));
            var configured = false;

            builder.WithRedisTasks(sp => sp.GetRequiredService<ClaimsPrincipal>(), configureTasks: _ => configured = true);

            using var provider = builder.Services.BuildServiceProvider();
            foreach (var configure in provider.GetServices<IConfigureOptions<McpServerOptions>>())
            {
                if (configure is ConfigureOptions<McpServerOptions>)
                {
                    configure.Configure(new McpServerOptions());
                }
            }

            configured.Should().BeTrue();
        }

        [TestMethod]
        public void WithRedisTasks_WhenUserIdIsWhitespace_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithRedisTasks("  ");

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
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithRedisTasks((Func<IServiceProvider, ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public async Task WithRedisTasks_FromUserIdFactory_CreatesTask()
        {
            using var redis = new RedisStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton<IConnectionMultiplexer>(redis.Multiplexer);

            builder.WithRedisTasks(() => "alice");

            using var provider = builder.Services.BuildServiceProvider();
            foreach (var configure in provider.GetServices<IConfigureOptions<McpServerOptions>>())
            {
                if (configure is ConfigureOptions<McpServerOptions>)
                {
                    configure.Configure(new McpServerOptions());
                }
            }

            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task WithRedisTasks_FromPrincipalFactory_CreatesTask()
        {
            using var redis = new RedisStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton<IConnectionMultiplexer>(redis.Multiplexer);

            builder.WithRedisTasks(() => TestUser.Authenticated("alice"));

            using var provider = builder.Services.BuildServiceProvider();
            foreach (var configure in provider.GetServices<IConfigureOptions<McpServerOptions>>())
            {
                if (configure is ConfigureOptions<McpServerOptions>)
                {
                    configure.Configure(new McpServerOptions());
                }
            }

            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        #endregion

        #region Private Methods

        private void ConfigureRedisHost(Action<IServiceCollection> configure)
        {
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
