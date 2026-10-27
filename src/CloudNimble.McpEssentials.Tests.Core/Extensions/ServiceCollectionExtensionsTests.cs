using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Tests.Core.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Server;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Core.Extensions
{

    /// <summary>
    /// Breakdance integration coverage for DI registration of authenticated task stores.
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
        public async Task AddInMemoryAuthenticatedTaskStore_ConstantUser_ResolvesSingletonStore()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddInMemoryAuthenticatedTaskStore("alice");
            });
            TestSetup();

            var store1 = GetService<IMcpTaskStore>();
            var store2 = GetService<IPerUserMcpTaskStore>();
            var concrete = GetService<InMemoryAuthenticatedMcpTaskStore>();

            store1.Should().BeSameAs(store2);
            store1.Should().BeSameAs(concrete);
            store1.Should().BeOfType<InMemoryAuthenticatedMcpTaskStore>();

            var created = await store1!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task AddInMemoryAuthenticatedTaskStore_FromUserIdFactory_UsesCallback()
        {
            var userId = "alice";
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddInMemoryAuthenticatedTaskStore(() => userId);
            });
            TestSetup();

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            userId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task AddInMemoryAuthenticatedTaskStore_FromPrincipalFactory_UsesClaims()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddInMemoryAuthenticatedTaskStore(_ => TestUser.Authenticated("principal-1"));
            });
            TestSetup();

            var store = GetService<IMcpTaskStore>();
            var perUser = GetService<IPerUserMcpTaskStore>();
            store.Should().BeSameAs(perUser);

            var created = await store!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void AddInMemoryAuthenticatedTaskStore_WhenServicesIsNull_Throws()
        {
            IServiceCollection services = null!;

            Action act = () => services.AddInMemoryAuthenticatedTaskStore("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [TestMethod]
        public void AddInMemoryAuthenticatedTaskStore_WhenUserIdIsWhitespace_Throws()
        {
            var services = new ServiceCollection();

            Action act = () => services.AddInMemoryAuthenticatedTaskStore("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void AddInMemoryAuthenticatedTaskStore_WhenGetCurrentUserIdIsNull_Throws()
        {
            var services = new ServiceCollection();

            Action act = () => services.AddInMemoryAuthenticatedTaskStore((Func<string?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void AddInMemoryAuthenticatedTaskStore_WhenPrincipalFactoryIsNull_Throws()
        {
            var services = new ServiceCollection();

            Action act = () => services.AddInMemoryAuthenticatedTaskStore((Func<IServiceProvider, ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public async Task WithAuthenticatedTasks_ConstantUser_CreatesIsolatedStore()
        {
            var builder = new TestMcpServerBuilder();

            builder.WithAuthenticatedTasks("alice");

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var perUser = provider.GetRequiredService<IPerUserMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeOfType<InMemoryAuthenticatedMcpTaskStore>();
            store.Should().BeSameAs(perUser);
        }

        [TestMethod]
        public async Task WithAuthenticatedTasks_FromUserIdFactory_UsesCallback()
        {
            var builder = new TestMcpServerBuilder();
            var userId = "alice";

            builder.WithAuthenticatedTasks(() => userId);

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            userId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task WithAuthenticatedTasks_FromPrincipal_UsesClaims()
        {
            var builder = new TestMcpServerBuilder();

            builder.WithAuthenticatedTasks(() => TestUser.Authenticated("principal-1"));

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task AddInMemoryAuthenticatedTaskStore_Configure_SetsDefaultTimeToLive()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddInMemoryAuthenticatedTaskStore("alice", o =>
                {
                    o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                    o.DefaultPollIntervalMs = 2500;
                });
            });
            TestSetup();

            var store = GetService<InMemoryAuthenticatedMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public async Task WithAuthenticatedTasks_Configure_SetsDefaultTimeToLive()
        {
            var builder = new TestMcpServerBuilder();

            builder.WithAuthenticatedTasks("alice", o =>
            {
                o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                o.DefaultPollIntervalMs = 2500;
            });

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<InMemoryAuthenticatedMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public void WithAuthenticatedTasks_ConfigureTasks_IsInvoked()
        {
            var builder = new TestMcpServerBuilder();
            var called = false;

            builder.WithAuthenticatedTasks("alice", configureTasks: _ => called = true);

            called.Should().BeTrue();
        }

        [TestMethod]
        public void WithAuthenticatedTasks_WhenBuilderIsNull_Throws()
        {
            IMcpServerBuilder builder = null!;

            Action act = () => builder.WithAuthenticatedTasks("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        [TestMethod]
        public void WithAuthenticatedTasks_WhenUserIdIsWhitespace_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithAuthenticatedTasks("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void WithAuthenticatedTasks_WhenGetCurrentUserIdIsNull_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithAuthenticatedTasks((Func<string?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void WithAuthenticatedTasks_WhenPrincipalFactoryIsNull_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithAuthenticatedTasks((Func<ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public async Task WithAuthenticatedTasks_FromServiceProvider_ResolvesRegisteredPrincipal()
        {
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));

            builder.WithAuthenticatedTasks(sp => sp.GetRequiredService<ClaimsPrincipal>());

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var perUser = provider.GetRequiredService<IPerUserMcpTaskStore>();
            var concrete = provider.GetRequiredService<InMemoryAuthenticatedMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeSameAs(perUser);
            store.Should().BeSameAs(concrete);
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public void WithAuthenticatedTasks_FromServiceProvider_CapturesProviderWhenOptionsAreConfigured()
        {
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));
            var configured = false;

            builder.WithAuthenticatedTasks(sp => sp.GetRequiredService<ClaimsPrincipal>(), configureTasks: _ => configured = true);

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
        public async Task WithAuthenticatedTasks_FromServiceProvider_IsolatesWhenResolvedUserChanges()
        {
            var builder = new TestMcpServerBuilder();
            var current = new TestUser("alice");
            builder.Services.AddSingleton(current);

            builder.WithAuthenticatedTasks(sp => TestUser.Authenticated(sp.GetRequiredService<TestUser>().UserId!));

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            current.UserId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public void WithAuthenticatedTasks_WhenServiceProviderFactoryIsNull_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithAuthenticatedTasks((Func<IServiceProvider, ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void WithAuthenticatedTasks_WhenBuilderIsNull_ServiceProviderFactory_Throws()
        {
            IMcpServerBuilder builder = null!;

            Action act = () => builder.WithAuthenticatedTasks((Func<IServiceProvider, ClaimsPrincipal?>)(_ => null));

            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        #endregion

    }

}
