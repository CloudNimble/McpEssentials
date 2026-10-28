using System;
using System.Linq;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// Shared in-memory SQLite database plus a built provider that hosts
    /// <see cref="McpTaskDbContext"/> as scoped, matching a real ASP.NET Core host.
    /// </summary>
    internal class SqliteStoreTestContext : IDisposable
    {

        #region Properties

        public SqliteConnection Connection { get; }

        public ServiceProvider Provider { get; }

        public IServiceScopeFactory Scopes => Provider.GetRequiredService<IServiceScopeFactory>();

        #endregion

        #region Constructors

        public SqliteStoreTestContext()
        {
            Connection = new SqliteConnection($"Data Source=file:mcp-{Guid.NewGuid():N}?mode=memory&cache=shared");
            Connection.Open();

            var services = new ServiceCollection();
            services.AddDbContext<McpTaskDbContext>(options => options.UseSqlite(Connection));
            Provider = services.BuildServiceProvider();

            using var scope = Provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<McpTaskDbContext>().Database.EnsureCreated();
        }

        #endregion

        #region Public Methods

        public EFCoreMcpTaskStore<McpTaskDbContext> CreateStore(
            Func<string?> getCurrentUserId,
            IMcpTaskInputSignalBus? signalBus = null)
            => new(Scopes, getCurrentUserId, signalBus);

        public EFCoreMcpTaskStore<McpTaskDbContext> CreateStore(
            string? userId,
            IMcpTaskInputSignalBus? signalBus = null)
            => new(Scopes, userId, signalBus);

        public int CountRows()
        {
            using var scope = Provider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<McpTaskDbContext>().Set<McpTaskEntity>().Count();
        }

        public void Dispose()
        {
            Provider.Dispose();
            Connection.Dispose();
        }

        #endregion

    }

}
