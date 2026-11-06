using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
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
        public async Task AddInMemoryMcpTaskStorePlus_ConstantUser_ResolvesSingletonStore()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddInMemoryMcpTaskStorePlus("alice");
            });
            TestSetup();

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
        public async Task AddInMemoryMcpTaskStorePlus_FromUserIdFactory_UsesCallback()
        {
            var userId = "alice";
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddInMemoryMcpTaskStorePlus(() => userId);
            });
            TestSetup();

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            userId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task AddInMemoryMcpTaskStorePlus_FromPrincipalFactory_UsesClaims()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddInMemoryMcpTaskStorePlus(_ => TestUser.Authenticated("principal-1"));
            });
            TestSetup();

            var store = GetService<IMcpTaskStore>();
            var perUser = GetService<IMcpTaskStorePlus>();
            store.Should().BeSameAs(perUser);

            var created = await store!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void AddInMemoryMcpTaskStorePlus_WhenServicesIsNull_Throws()
        {
            IServiceCollection services = null!;

            Action act = () => services.AddInMemoryMcpTaskStorePlus("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [TestMethod]
        public void AddInMemoryMcpTaskStorePlus_WhenUserIdIsWhitespace_Throws()
        {
            var services = new ServiceCollection();

            Action act = () => services.AddInMemoryMcpTaskStorePlus("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void AddInMemoryMcpTaskStorePlus_WhenGetCurrentUserIdIsNull_Throws()
        {
            var services = new ServiceCollection();

            Action act = () => services.AddInMemoryMcpTaskStorePlus((Func<string?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void AddInMemoryMcpTaskStorePlus_WhenPrincipalFactoryIsNull_Throws()
        {
            var services = new ServiceCollection();

            Action act = () => services.AddInMemoryMcpTaskStorePlus((Func<IServiceProvider, ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        /// <summary>
        /// <c>WithTasksPlus</c> provider picker uses <c>UseInMemory</c>.
        /// </summary>
        [TestMethod]
        public async Task WithTasksPlus_UseInMemory_CreatesIsolatedStore()
        {
            var builder = new TestMcpServerBuilder();

            builder.WithTasksPlus(tasks => tasks.UseInMemory("alice"));

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeOfType<InMemoryMcpTaskStorePlus>();
        }

        /// <summary>
        /// Constant user id creates an isolated in-memory store.
        /// </summary>
        [TestMethod]
        public async Task WithTasksPlus_ConstantUser_CreatesIsolatedStore()
        {
            var builder = new TestMcpServerBuilder();

            builder.WithTasksPlus("alice");

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var perUser = provider.GetRequiredService<IMcpTaskStorePlus>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeOfType<InMemoryMcpTaskStorePlus>();
            store.Should().BeSameAs(perUser);
        }

        [TestMethod]
        public async Task WithTasksPlus_FromUserIdFactory_UsesCallback()
        {
            var builder = new TestMcpServerBuilder();
            var userId = "alice";

            builder.WithTasksPlus(() => userId);

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            userId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task WithTasksPlus_FromPrincipal_UsesClaims()
        {
            var builder = new TestMcpServerBuilder();

            builder.WithTasksPlus(() => TestUser.Authenticated("principal-1"));

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task AddInMemoryMcpTaskStorePlus_Configure_SetsDefaultTimeToLive()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddInMemoryMcpTaskStorePlus("alice", o =>
                {
                    o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                    o.DefaultPollIntervalMs = 2500;
                });
            });
            TestSetup();

            var store = GetService<InMemoryMcpTaskStorePlus>();
            var created = await store!.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public async Task WithTasksPlus_Configure_SetsDefaultTimeToLive()
        {
            var builder = new TestMcpServerBuilder();

            builder.WithTasksPlus("alice", o =>
            {
                o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                o.DefaultPollIntervalMs = 2500;
            });

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<InMemoryMcpTaskStorePlus>();
            var created = await store.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public void WithTasksPlus_ConfigureTasks_IsInvoked()
        {
            var builder = new TestMcpServerBuilder();
            var called = false;

            builder.WithTasksPlus("alice", configureTasks: _ => called = true);

            called.Should().BeTrue();
        }

        [TestMethod]
        public void WithTasksPlus_WhenBuilderIsNull_Throws()
        {
            IMcpServerBuilder builder = null!;

            Action act = () => builder.WithTasksPlus("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        [TestMethod]
        public void WithTasksPlus_WhenUserIdIsWhitespace_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithTasksPlus("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void WithTasksPlus_WhenGetCurrentUserIdIsNull_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithTasksPlus((Func<string?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void WithTasksPlus_WhenPrincipalFactoryIsNull_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithTasksPlus((Func<ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public async Task WithTasksPlus_FromServiceProvider_ResolvesRegisteredPrincipal()
        {
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));

            builder.WithTasksPlus(sp => sp.GetRequiredService<ClaimsPrincipal>());

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var perUser = provider.GetRequiredService<IMcpTaskStorePlus>();
            var concrete = provider.GetRequiredService<InMemoryMcpTaskStorePlus>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeSameAs(perUser);
            store.Should().BeSameAs(concrete);
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public void WithTasksPlus_FromServiceProvider_CapturesProviderWhenOptionsAreConfigured()
        {
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));
            var configured = false;

            builder.WithTasksPlus(sp => sp.GetRequiredService<ClaimsPrincipal>(), configureTasks: _ => configured = true);

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
        public async Task WithTasksPlus_FromServiceProvider_IsolatesWhenResolvedUserChanges()
        {
            var builder = new TestMcpServerBuilder();
            var current = new TestUser("alice");
            builder.Services.AddSingleton(current);

            builder.WithTasksPlus(sp => TestUser.Authenticated(sp.GetRequiredService<TestUser>().UserId!));

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            current.UserId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public void WithTasksPlus_WhenServiceProviderFactoryIsNull_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithTasksPlus((Func<IServiceProvider, ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void WithTasksPlus_WhenBuilderIsNull_ServiceProviderFactory_Throws()
        {
            IMcpServerBuilder builder = null!;

            Action act = () => builder.WithTasksPlus((Func<IServiceProvider, ClaimsPrincipal?>)(_ => null));

            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        [TestMethod]
        public void AddFormElicitation_ResolvesKeyedSingleton()
        {
            var form = FormElicitationBuilder.Create("Ship this order")
                .String("trackingNumber").Required();
            var services = new ServiceCollection();
            services.AddFormElicitation("ship", form);

            using var provider = services.BuildServiceProvider();
            var resolved = provider.GetRequiredKeyedService<IFormElicitationSource>("ship");

            resolved.Should().BeSameAs(form);
            resolved.Build().Message.Should().Be("Ship this order");
        }

        [TestMethod]
        public void AddFormElicitation_StampsNameWhenEmpty()
        {
            var form = FormElicitationBuilder.Create("Ship this order");
            form.String("trackingNumber").Required();
            var services = new ServiceCollection();
            services.AddFormElicitation("ship", form);

            form.Name.Should().Be("ship");
            form.ToXml().Should().Contain("Name=\"ship\"");
        }

        [TestMethod]
        public void AddFormElicitation_StampsNameThroughFieldFacade()
        {
            var form = FormElicitationBuilder.Create("Ship this order");
            var facade = form.String("trackingNumber").Required();
            var services = new ServiceCollection();
            services.AddFormElicitation("ship", facade);

            form.Name.Should().Be("ship");
            facade.ToXml().Should().Contain("Name=\"ship\"");
        }

        [TestMethod]
        public void AddFormElicitation_DoesNotOverwriteExistingName()
        {
            var form = FormElicitationBuilder.Create("Ship this order");
            form.String("trackingNumber").Required();
            form.Name = "dispatch";
            var services = new ServiceCollection();
            services.AddFormElicitation("ship", form);

            form.Name.Should().Be("dispatch");
        }

        [TestMethod]
        public void AddFormElicitation_DoesNotStampSealedForm()
        {
            var form = FormElicitationBuilder.Create("Ship this order");
            form.String("trackingNumber").Required();
            _ = form.Build();
            var services = new ServiceCollection();
            services.AddFormElicitation("ship", form);

            form.Name.Should().BeNull();
        }

        [TestMethod]
        public void AddFormElicitation_SameInstance_CanRegisterUnderTwoNames()
        {
            var form = FormElicitationBuilder.Create("Ship this order").String("trackingNumber").Required();
            var services = new ServiceCollection();
            services.AddFormElicitation("ship", form);
            services.AddFormElicitation("dispatch", form);

            using var provider = services.BuildServiceProvider();
            provider.GetRequiredKeyedService<IFormElicitationSource>("ship").Should().BeSameAs(form);
            provider.GetRequiredKeyedService<IFormElicitationSource>("dispatch").Should().BeSameAs(form);
        }

        [TestMethod]
        public void AddFormElicitation_BlankName_ThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var form = FormElicitationBuilder.Create("m");
            var act = () => services.AddFormElicitation("  ", form);
            act.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        [TestMethod]
        public void AddFormElicitation_NullForm_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            var act = () => services.AddFormElicitation("ship", null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("form");
        }

        [TestMethod]
        public void AddFormElicitation_NullServices_ThrowsArgumentNullException()
        {
            IServiceCollection services = null!;
            var act = () => services.AddFormElicitation("ship", FormElicitationBuilder.Create("m"));
            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [TestMethod]
        public void AddFormElicitation_DuplicateName_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var first = FormElicitationBuilder.Create("Ship this order").String("trackingNumber").Required();
            var second = FormElicitationBuilder.Create("Dispatch").String("route").Required();
            services.AddFormElicitation("ship", first);

            var act = () => services.AddFormElicitation("ship", second);
            act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
        }

        [TestMethod]
        public void AddFormElicitation_IgnoresUnkeyedAndNonStringKeys()
        {
            var form = FormElicitationBuilder.Create("Ship this order").String("trackingNumber").Required();
            var services = new ServiceCollection();
            services.AddSingleton<IFormElicitationSource>(form);
            services.AddKeyedSingleton<IFormElicitationSource>(42, form);
            services.AddFormElicitation("ship", form);

            using var provider = services.BuildServiceProvider();
            provider.GetRequiredKeyedService<IFormElicitationSource>("ship").Should().BeSameAs(form);
        }

        [TestMethod]
        public void AddFormElicitation_DuplicateName_AfterOtherDescriptors_ThrowsInvalidOperationException()
        {
            var form = FormElicitationBuilder.Create("Ship this order").String("trackingNumber").Required();
            var services = new ServiceCollection();
            services.AddSingleton(form);
            services.AddKeyedSingleton<IFormElicitationSource>("dispatch", form);
            services.AddFormElicitation("ship", form);

            var act = () => services.AddFormElicitation("ship", FormElicitationBuilder.Create("other"));
            act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
        }

        [TestMethod]
        public void WithTasksPlus_WhenConfigureDoesNotSelectStore_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithTasksPlus(_ => { });

            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public async Task WithTasksPlus_UseInMemoryFromServiceProvider_ResolvesUser()
        {
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton("alice");
            builder.WithTasksPlus(tasks => tasks.UseInMemory(sp => sp.GetRequiredService<string>()));

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

    }

}
