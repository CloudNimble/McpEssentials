using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using CloudNimble.McpEssentials.Tests.Cosmos.Tasks;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Server;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Extensions
{

    /// <summary>
    /// Breakdance coverage for Cosmos store DI registration and WithCosmosTasks.
    /// Does not call the store; a dummy <see cref="CosmosClient"/> is enough.
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsTests : BreakdanceMSTestBase
    {

        #region Test Lifecycle

        [TestCleanup]
        public void TearDown()
        {
            TestTearDown();
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public void AddMcpEssentialsCosmosTasks_ConstantUser_ResolvesSingletonStore()
        {
            ConfigureCosmosHost(services =>
            {
                services.AddMcpEssentialsCosmosTasks("alice");
            });

            var store1 = GetService<IMcpTaskStore>();
            var store2 = GetService<IMcpTaskStorePlus>();
            var concrete = GetService<CosmosMcpTaskStore>();

            store1.Should().BeSameAs(store2);
            store1.Should().BeSameAs(concrete);
            concrete!.AutoSweep.Should().BeFalse();
        }

        [TestMethod]
        public void AddMcpEssentialsCosmosTasks_FromPrincipalFactory_ResolvesStore()
        {
            ConfigureCosmosHost(services =>
            {
                services.AddMcpEssentialsCosmosTasks(_ => TestUser.Authenticated("principal-1"));
            });

            GetService<IMcpTaskStore>().Should().BeOfType<CosmosMcpTaskStore>();
        }

        [TestMethod]
        public void AddMcpEssentialsCosmosTasks_WhenServicesIsNull_Throws()
        {
            IServiceCollection services = null!;

            Action act = () => services.AddMcpEssentialsCosmosTasks("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [TestMethod]
        public void AddMcpEssentialsCosmosTasks_WhenUserIdIsWhitespace_Throws()
        {
            var services = new ServiceCollection();

            Action act = () => services.AddMcpEssentialsCosmosTasks("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void AddMcpEssentialsCosmosTasks_Configure_SetsDatabaseAndTtl()
        {
            ConfigureCosmosHost(services =>
            {
                services.AddMcpEssentialsCosmosTasks("alice", o =>
                {
                    o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                    o.DefaultPollIntervalMs = 2500;
                    o.DatabaseId = "app";
                    o.TasksContainerId = "Tasks";
                    o.IndexContainerId = "Index";
                    o.Throughput = 1000;
                });
            });

            var store = GetService<CosmosMcpTaskStore>();

            store!.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            store.DatabaseId.Should().Be("app");
            store.TasksContainerId.Should().Be("Tasks");
            store.IndexContainerId.Should().Be("Index");
            store.Throughput.Should().Be(1000);
            store.AutoSweep.Should().BeFalse();
        }

        [TestMethod]
        public void WithCosmosTasks_Configure_SetsDefaultTimeToLive()
        {
            using var client = CosmosStoreTestContext.CreateDummyClient();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(client);

            builder.WithCosmosTasks("alice", o =>
            {
                o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                o.DefaultPollIntervalMs = 2500;
            });

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<CosmosMcpTaskStore>();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public void WithCosmosTasks_ConstantUser_RegistersIsolatedStore()
        {
            using var client = CosmosStoreTestContext.CreateDummyClient();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(client);

            builder.WithCosmosTasks("alice");

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var perUser = provider.GetRequiredService<IMcpTaskStorePlus>();

            store.Should().BeOfType<CosmosMcpTaskStore>();
            store.Should().BeSameAs(perUser);
        }

        [TestMethod]
        public void WithCosmosTasks_FromServiceProvider_CapturesProviderWhenOptionsAreConfigured()
        {
            using var client = CosmosStoreTestContext.CreateDummyClient();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(client);
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));
            var configured = false;

            builder.WithCosmosTasks(sp => sp.GetRequiredService<ClaimsPrincipal>(), configureTasks: _ => configured = true);

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
        public void WithCosmosTasks_WhenUserIdIsWhitespace_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithCosmosTasks("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void WithCosmosTasks_WhenBuilderIsNull_Throws()
        {
            IMcpServerBuilder builder = null!;

            Action act = () => builder.WithCosmosTasks("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        [TestMethod]
        public void WithCosmosTasks_WhenPrincipalFactoryIsNull_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithCosmosTasks((Func<IServiceProvider, ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void Constructor_WhenClientIsNull_Throws()
        {
            Action act = () => _ = new CosmosMcpTaskStore((CosmosClient)null!, "alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("client");
        }

        [TestMethod]
        public void Constructor_WhenGetCurrentUserIdIsNull_Throws()
        {
            using var client = CosmosStoreTestContext.CreateDummyClient();

            Action act = () => _ = new CosmosMcpTaskStore(client, (Func<string?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public async Task WithCosmosTasks_FromUserIdFactory_CreatesTask()
        {
            using var client = CosmosStoreTestContext.CreateDummyClient();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(client);
            builder.Services.AddSingleton<ICosmosTaskGateway>(new InMemoryCosmosTaskGateway());

            builder.WithCosmosTasks(() => "alice");

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
        public async Task WithCosmosTasks_FromPrincipalFactory_CreatesTask()
        {
            using var client = CosmosStoreTestContext.CreateDummyClient();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(client);
            builder.Services.AddSingleton<ICosmosTaskGateway>(new InMemoryCosmosTaskGateway());

            builder.WithCosmosTasks(() => TestUser.Authenticated("alice"));

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
        public async Task AddMcpEssentialsCosmosTasks_FromUserIdFactory_CreatesTask()
        {
            ConfigureCosmosHost(services =>
            {
                services.AddMcpEssentialsCosmosTasks(() => "alice");
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        #endregion

        #region Private Methods

        private void ConfigureCosmosHost(Action<IServiceCollection> configure)
        {
            var client = CosmosStoreTestContext.CreateDummyClient();
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(client);
                services.AddSingleton<ICosmosTaskGateway>(new InMemoryCosmosTaskGateway());
                configure(services);
            });
            TestSetup();
        }

        #endregion

    }

}
