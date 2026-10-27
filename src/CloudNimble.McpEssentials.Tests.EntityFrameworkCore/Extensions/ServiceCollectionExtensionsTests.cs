using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Server;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Extensions
{

    /// <summary>
    /// Breakdance coverage for EF Core store DI registration and WithEntityFrameworkTasks.
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsTests : BreakdanceMSTestBase
    {

        #region Fields

        private SqliteConnection? _connection;

        #endregion

        #region Test Lifecycle

        [TestCleanup]
        public void TearDown()
        {
            TestTearDown();
            _connection?.Dispose();
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public async Task AddMcpEssentialsEntityFrameworkTasks_ConstantUser_ResolvesSingletonStore()
        {
            ConfigureSqliteHost(services =>
            {
                services.AddMcpEssentialsEntityFrameworkTasks<McpTaskDbContext>("alice");
            });

            var store1 = GetService<IMcpTaskStore>();
            var store2 = GetService<IPerUserMcpTaskStore>();
            var concrete = GetService<EntityFrameworkAuthenticatedTaskStore<McpTaskDbContext>>();

            store1.Should().BeSameAs(store2);
            store1.Should().BeSameAs(concrete);

            var created = await store1!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task AddMcpEssentialsEntityFrameworkTasks_FromUserIdFactory_UsesCallback()
        {
            var userId = "alice";
            ConfigureSqliteHost(services =>
            {
                services.AddMcpEssentialsEntityFrameworkTasks<McpTaskDbContext>(() => userId);
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();

            userId = "bob";
            (await store.GetTaskAsync(created.TaskId)).Should().BeNull();
        }

        [TestMethod]
        public async Task AddMcpEssentialsEntityFrameworkTasks_FromPrincipalFactory_UsesClaims()
        {
            ConfigureSqliteHost(services =>
            {
                services.AddMcpEssentialsEntityFrameworkTasks<McpTaskDbContext>(_ => TestUser.Authenticated("principal-1"));
            });

            var store = GetService<IMcpTaskStore>();
            var created = await store!.CreateTaskAsync();
            created.TaskId.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void AddMcpEssentialsEntityFrameworkTasks_WhenServicesIsNull_Throws()
        {
            IServiceCollection services = null!;

            Action act = () => services.AddMcpEssentialsEntityFrameworkTasks<McpTaskDbContext>("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [TestMethod]
        public void AddMcpEssentialsEntityFrameworkTasks_WhenUserIdIsWhitespace_Throws()
        {
            var services = new ServiceCollection();

            Action act = () => services.AddMcpEssentialsEntityFrameworkTasks<McpTaskDbContext>("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public async Task AddMcpEssentialsEntityFrameworkTasks_Configure_SetsDefaultTimeToLive()
        {
            ConfigureSqliteHost(services =>
            {
                services.AddMcpEssentialsEntityFrameworkTasks<McpTaskDbContext>("alice", o =>
                {
                    o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                    o.DefaultPollIntervalMs = 2500;
                });
            });

            var store = GetService<EntityFrameworkAuthenticatedTaskStore<McpTaskDbContext>>();
            var created = await store!.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public async Task WithEntityFrameworkTasks_Configure_SetsDefaultTimeToLive()
        {
            using var sqlite = new SqliteStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(sqlite.Connection);
            builder.Services.AddDbContext<McpTaskDbContext>(o => o.UseSqlite(sqlite.Connection));

            builder.WithEntityFrameworkTasks<McpTaskDbContext>("alice", o =>
            {
                o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                o.DefaultPollIntervalMs = 2500;
            });

            using var provider = builder.Services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<McpTaskDbContext>().Database.EnsureCreated();
            var store = provider.GetRequiredService<EntityFrameworkAuthenticatedTaskStore<McpTaskDbContext>>();
            var created = await store.CreateTaskAsync();

            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.DefaultPollIntervalMs.Should().Be(2500);
            created.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            created.PollIntervalMs.Should().Be(2500);
        }

        [TestMethod]
        public async Task WithEntityFrameworkTasks_ConstantUser_CreatesIsolatedStore()
        {
            using var sqlite = new SqliteStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(sqlite.Connection);
            builder.Services.AddDbContext<McpTaskDbContext>(o => o.UseSqlite(sqlite.Connection));

            builder.WithEntityFrameworkTasks<McpTaskDbContext>("alice");

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var perUser = provider.GetRequiredService<IPerUserMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            store.Should().BeOfType<EntityFrameworkAuthenticatedTaskStore<McpTaskDbContext>>();
            store.Should().BeSameAs(perUser);
        }

        [TestMethod]
        public async Task WithEntityFrameworkTasks_FromServiceProvider_ResolvesRegisteredPrincipal()
        {
            using var sqlite = new SqliteStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(sqlite.Connection);
            builder.Services.AddDbContext<McpTaskDbContext>(o => o.UseSqlite(sqlite.Connection));
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));

            builder.WithEntityFrameworkTasks<McpTaskDbContext>(sp => sp.GetRequiredService<ClaimsPrincipal>());

            using var provider = builder.Services.BuildServiceProvider();
            var store = provider.GetRequiredService<IMcpTaskStore>();
            var created = await store.CreateTaskAsync();

            created.TaskId.Should().NotBeNullOrWhiteSpace();
            (await store.GetTaskAsync(created.TaskId)).Should().NotBeNull();
        }

        [TestMethod]
        public void WithEntityFrameworkTasks_FromServiceProvider_CapturesProviderWhenOptionsAreConfigured()
        {
            using var sqlite = new SqliteStoreTestContext();
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(sqlite.Connection);
            builder.Services.AddDbContext<McpTaskDbContext>(o => o.UseSqlite(sqlite.Connection));
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));
            var configured = false;

            builder.WithEntityFrameworkTasks<McpTaskDbContext>(sp => sp.GetRequiredService<ClaimsPrincipal>(), configureTasks: _ => configured = true);

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
        public void WithEntityFrameworkTasks_WhenUserIdIsWhitespace_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithEntityFrameworkTasks<McpTaskDbContext>("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void WithEntityFrameworkTasks_WhenBuilderIsNull_Throws()
        {
            IMcpServerBuilder builder = null!;

            Action act = () => builder.WithEntityFrameworkTasks<McpTaskDbContext>("alice");

            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        [TestMethod]
        public void WithEntityFrameworkTasks_WhenPrincipalFactoryIsNull_Throws()
        {
            var builder = new TestMcpServerBuilder();

            Action act = () => builder.WithEntityFrameworkTasks<McpTaskDbContext>((Func<IServiceProvider, ClaimsPrincipal?>)null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void AddMcpTasksTable_WhenModelBuilderIsNull_Throws()
        {
            ModelBuilder builder = null!;

            Action act = () => builder.AddMcpTasksTable();

            act.Should().Throw<ArgumentNullException>().WithParameterName("modelBuilder");
        }

        [TestMethod]
        public void AddMcpTasksTable_WhenTableNameIsWhitespace_Throws()
        {
            var builder = new ModelBuilder();

            Action act = () => builder.AddMcpTasksTable("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("tableName");
        }

        [TestMethod]
        public void AddMcpTasksTable_CustomTableAndSchema_MapsBoth()
        {
            var builder = new ModelBuilder();
            builder.AddMcpTasksTable("AppTasks", "mcp");

            var entity = builder.Model.FindEntityType(typeof(McpTaskEntity));
            entity.Should().NotBeNull();
            var mapped = entity!;
            mapped.GetTableName().Should().Be("AppTasks");
            mapped.GetSchema().Should().Be("mcp");
        }

        #endregion

        #region Private Methods

        private void ConfigureSqliteHost(Action<IServiceCollection> configure)
        {
            _connection = new SqliteConnection($"Data Source=file:mcp-di-{Guid.NewGuid():N}?mode=memory&cache=shared");
            _connection.Open();
            var connection = _connection;
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(connection);
                services.AddDbContext<McpTaskDbContext>(o => o.UseSqlite(connection));
                configure(services);
            });
            TestSetup();
            using var scope = GetService<IServiceScopeFactory>()!.CreateScope();
            scope.ServiceProvider.GetRequiredService<McpTaskDbContext>().Database.EnsureCreated();
        }

        #endregion

    }

}
