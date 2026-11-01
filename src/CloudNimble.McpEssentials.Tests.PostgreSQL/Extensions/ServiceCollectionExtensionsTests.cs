using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Server;
using Npgsql;
using System;
using System.Security.Claims;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Extensions
{

    /// <summary>
    /// Breakdance coverage for Postgres store DI registration and WithPostgreSqlTasks.
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsTests : BreakdanceMSTestBase
    {

        #region Fields

        private PostgresStoreTestContext? _postgres;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup() => _postgres = new PostgresStoreTestContext();

        [TestCleanup]
        public void TearDown()
        {
            TestTearDown();
            _postgres?.Dispose();
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_ConstantUser_ResolvesSingletonStore()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_postgres!.DataSource);
            services.AddMcpEssentialsPostgreSqlTasks("alice");
            using var provider = services.BuildServiceProvider();

            var store1 = provider.GetRequiredService<IMcpTaskStore>();
            var store2 = provider.GetRequiredService<IMcpTaskStorePlus>();
            var concrete = provider.GetRequiredService<PostgreSqlMcpTaskStore>();
            store1.Should().BeSameAs(store2);
            store1.Should().BeSameAs(concrete);
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_FromUserIdFactory_Registers()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_postgres!.DataSource);
            services.AddMcpEssentialsPostgreSqlTasks(() => "alice");
            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<PostgreSqlMcpTaskStore>().Should().NotBeNull();
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_FromPrincipalFactory_Registers()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_postgres!.DataSource);
            services.AddMcpEssentialsPostgreSqlTasks(_ => TestUser.Authenticated("alice"));
            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IMcpTaskStore>().Should().BeOfType<PostgreSqlMcpTaskStore>();
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_Configure_SetsTable()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_postgres!.DataSource);
            services.AddMcpEssentialsPostgreSqlTasks("alice", o =>
            {
                o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                o.TableName = "AppTasks";
                o.Schema = "mcp";
                o.ListenChannel = "mcp_tasks";
            });
            using var provider = services.BuildServiceProvider();
            var store = provider.GetRequiredService<PostgreSqlMcpTaskStore>();
            store.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
            store.TableName.Should().Be("AppTasks");
            store.Schema.Should().Be("mcp");
            store.ListenChannel.Should().Be("mcp_tasks");
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_WhenServicesIsNull_Throws()
        {
            IServiceCollection services = null!;
            Action act = () => services.AddMcpEssentialsPostgreSqlTasks("alice");
            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_WhenUserIdIsWhitespace_Throws()
        {
            Action act = () => new ServiceCollection().AddMcpEssentialsPostgreSqlTasks("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_WhenGetCurrentUserIdIsNull_Throws()
        {
            Action act = () => new ServiceCollection().AddMcpEssentialsPostgreSqlTasks((Func<string?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_WhenGetPrincipalIsNull_Throws()
        {
            Action act = () => new ServiceCollection().AddMcpEssentialsPostgreSqlTasks((Func<IServiceProvider, ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void WithPostgreSqlTasks_ConstantUser_Registers()
        {
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(_postgres!.DataSource);
            builder.WithPostgreSqlTasks("alice");
            using var provider = builder.Services.BuildServiceProvider();
            provider.GetRequiredService<IMcpTaskStore>().Should().BeOfType<PostgreSqlMcpTaskStore>();
        }

        [TestMethod]
        public void WithPostgreSqlTasks_FromUserIdFactory_Registers()
        {
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(_postgres!.DataSource);
            builder.WithPostgreSqlTasks(() => "alice");
            using var provider = builder.Services.BuildServiceProvider();
            provider.GetRequiredService<PostgreSqlMcpTaskStore>().Should().NotBeNull();
        }

        [TestMethod]
        public void WithPostgreSqlTasks_FromPrincipal_Registers()
        {
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(_postgres!.DataSource);
            builder.WithPostgreSqlTasks(() => TestUser.Authenticated("alice"));
            using var provider = builder.Services.BuildServiceProvider();
            provider.GetRequiredService<IMcpTaskStorePlus>().Should().NotBeNull();
        }

        [TestMethod]
        public void WithPostgreSqlTasks_FromServiceProvider_CapturesProviderWhenOptionsAreConfigured()
        {
            var builder = new TestMcpServerBuilder();
            builder.Services.AddSingleton(_postgres!.DataSource);
            builder.Services.AddSingleton(TestUser.Authenticated("alice"));
            var configured = false;
            builder.WithPostgreSqlTasks(
                sp => sp.GetRequiredService<ClaimsPrincipal>(),
                o => o.TableName = "AppTasks",
                _ => configured = true);

            using var provider = builder.Services.BuildServiceProvider();
            foreach (var configure in provider.GetServices<IConfigureOptions<McpServerOptions>>())
            {
                if (configure is ConfigureOptions<McpServerOptions>)
                {
                    configure.Configure(new McpServerOptions());
                }
            }

            configured.Should().BeTrue();
            provider.GetRequiredService<PostgreSqlMcpTaskStore>().TableName.Should().Be("AppTasks");
        }

        [TestMethod]
        public void WithPostgreSqlTasks_WhenUserIdIsWhitespace_Throws()
        {
            Action act = () => new TestMcpServerBuilder().WithPostgreSqlTasks("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void WithPostgreSqlTasks_WhenGetCurrentUserIdIsNull_Throws()
        {
            Action act = () => new TestMcpServerBuilder().WithPostgreSqlTasks((Func<string?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void WithPostgreSqlTasks_WhenGetPrincipalIsNull_Throws()
        {
            Action act = () => new TestMcpServerBuilder().WithPostgreSqlTasks((Func<ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void WithPostgreSqlTasks_WhenServiceProviderFactoryIsNull_Throws()
        {
            Action act = () => new TestMcpServerBuilder().WithPostgreSqlTasks((Func<IServiceProvider, ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        #endregion

    }

}
