using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Extensions.Tasks;
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
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_postgres!.DataSource);
                services.AddMcpEssentialsPostgreSqlTasks("alice");
            });
            TestSetup();

            var store1 = GetService<IMcpTaskStore>();
            var store2 = GetService<IMcpTaskStorePlus>();
            var concrete = GetService<PostgreSqlMcpTaskStore>();
            store1.Should().BeSameAs(store2);
            store1.Should().BeSameAs(concrete);
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_FromUserIdFactory_Registers()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_postgres!.DataSource);
                services.AddMcpEssentialsPostgreSqlTasks(() => "alice");
            });
            TestSetup();
            GetService<PostgreSqlMcpTaskStore>().Should().NotBeNull();
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_FromPrincipalFactory_Registers()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_postgres!.DataSource);
                services.AddMcpEssentialsPostgreSqlTasks(_ => TestUser.Authenticated("alice"));
            });
            TestSetup();
            GetService<IMcpTaskStore>().Should().BeOfType<PostgreSqlMcpTaskStore>();
        }

        [TestMethod]
        public void AddMcpEssentialsPostgreSqlTasks_Configure_SetsTable()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_postgres!.DataSource);
                services.AddMcpEssentialsPostgreSqlTasks("alice", o =>
                {
                    o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                    o.TableName = "AppTasks";
                    o.Schema = "mcp";
                    o.ListenChannel = "mcp_tasks";
                });
            });
            TestSetup();
            var store = GetService<PostgreSqlMcpTaskStore>();
            store!.DefaultTimeToLive.Should().Be(TimeSpan.FromMinutes(5));
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
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_postgres!.DataSource);
                services.AddMcpServer().WithPostgreSqlTasks("alice");
            });
            TestSetup();
            GetService<IMcpTaskStore>().Should().BeOfType<PostgreSqlMcpTaskStore>();
        }

        [TestMethod]
        public void WithPostgreSqlTasks_FromUserIdFactory_Registers()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_postgres!.DataSource);
                services.AddMcpServer().WithPostgreSqlTasks(() => "alice");
            });
            TestSetup();
            GetService<PostgreSqlMcpTaskStore>().Should().NotBeNull();
        }

        [TestMethod]
        public void WithPostgreSqlTasks_FromPrincipal_Registers()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_postgres!.DataSource);
                services.AddMcpServer().WithPostgreSqlTasks(() => TestUser.Authenticated("alice"));
            });
            TestSetup();
            GetService<IMcpTaskStorePlus>().Should().NotBeNull();
        }

        [TestMethod]
        public void WithPostgreSqlTasks_FromServiceProvider_CapturesProviderWhenOptionsAreConfigured()
        {
            var configured = false;
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_postgres!.DataSource);
                services.AddSingleton(TestUser.Authenticated("alice"));
                services.AddMcpServer().WithPostgreSqlTasks(
                    sp => sp.GetRequiredService<ClaimsPrincipal>(),
                    o => o.TableName = "AppTasks",
                    _ => configured = true);
            });
            TestSetup();

            configured.Should().BeTrue();
            GetService<PostgreSqlMcpTaskStore>()!.TableName.Should().Be("AppTasks");
        }

        [TestMethod]
        public void WithPostgreSqlTasks_WhenUserIdIsWhitespace_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithPostgreSqlTasks("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void WithPostgreSqlTasks_WhenGetCurrentUserIdIsNull_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithPostgreSqlTasks((Func<string?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void WithPostgreSqlTasks_WhenGetPrincipalIsNull_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithPostgreSqlTasks((Func<ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void WithPostgreSqlTasks_WhenServiceProviderFactoryIsNull_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithPostgreSqlTasks((Func<IServiceProvider, ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        #endregion

    }

}
