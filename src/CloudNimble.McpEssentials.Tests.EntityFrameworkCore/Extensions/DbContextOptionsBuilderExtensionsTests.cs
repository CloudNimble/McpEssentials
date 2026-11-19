using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Extensions
{

    /// <summary>
    /// Coverage for mapping <see cref="McpTaskEntity"/> from <see cref="DbContextOptionsBuilder"/>
    /// so a host context does not call <c>AddMcpTasksTable</c> in <c>OnModelCreating</c>.
    /// </summary>
    [TestClass]
    public class DbContextOptionsBuilderExtensionsTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void AddMcpTasksTable_WhenOptionsBuilderIsNull_Throws()
        {
            DbContextOptionsBuilder builder = null!;

            Action act = () => builder.AddMcpTasksTable();

            act.Should().Throw<ArgumentNullException>().WithParameterName("optionsBuilder");
        }

        [TestMethod]
        public void AddMcpTasksTable_WhenTableNameIsWhitespace_Throws()
        {
            var builder = new DbContextOptionsBuilder();

            Action act = () => builder.AddMcpTasksTable("  ");

            act.Should().Throw<ArgumentException>().WithParameterName("tableName");
        }

        [TestMethod]
        public void AddMcpTasksTable_OnOptions_MapsEntityAfterHostOnModelCreating()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<HostOnlyDbContext>()
                .UseSqlite(connection)
                .AddMcpTasksTable()
                .Options;

            using var db = new HostOnlyDbContext(options);
            db.Database.EnsureCreated();

            var mapped = db.Model.FindEntityType(typeof(McpTaskEntity));
            mapped.Should().NotBeNull();
            mapped!.GetTableName().Should().Be("McpTasks");
            db.Model.FindEntityType(typeof(HostWidget)).Should().NotBeNull();
            db.Set<McpTaskEntity>().Count().Should().Be(0);
            db.Widgets.Should().NotBeNull();
        }

        [TestMethod]
        public void AddMcpTasksTable_OnOptions_CustomTableAndSchema_MapsBoth()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<HostOnlyDbContext>()
                .UseSqlite(connection)
                .AddMcpTasksTable("AppTasks", "mcp")
                .Options;

            using var db = new HostOnlyDbContext(options);

            var entity = db.Model.FindEntityType(typeof(McpTaskEntity));
            entity.Should().NotBeNull();
            var mapped = entity!;
            mapped.GetTableName().Should().Be("AppTasks");
            mapped.GetSchema().Should().Be("mcp");
        }

        [TestMethod]
        public void AddMcpTasksTable_GenericOverload_ReturnsTypedBuilder()
        {
            var builder = new DbContextOptionsBuilder<HostOnlyDbContext>();

            var returned = builder.AddMcpTasksTable("AppTasks");

            returned.Should().BeSameAs(builder);
        }

        #endregion

    }

}
