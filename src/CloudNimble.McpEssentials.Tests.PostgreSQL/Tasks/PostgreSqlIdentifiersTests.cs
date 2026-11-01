using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// Coverage for identifier quoting used in DDL and LISTEN.
    /// </summary>
    [TestClass]
    public class PostgreSqlIdentifiersTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Quote_SimpleName_WrapsInQuotes()
        {
            PostgreSqlIdentifiers.Quote("McpTasks").Should().Be("\"McpTasks\"");
            PostgreSqlIdentifiers.Quote("public").Should().Be("\"public\"");
            PostgreSqlIdentifiers.Quote("_channel").Should().Be("\"_channel\"");
            PostgreSqlIdentifiers.Quote("mcp_essentials_tasks").Should().Be("\"mcp_essentials_tasks\"");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void Quote_Blank_Throws(string? name)
        {
            Action act = () => PostgreSqlIdentifiers.Quote(name!);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Quote_StartsWithDigit_Throws()
        {
            Action act = () => PostgreSqlIdentifiers.Quote("1tasks");
            act.Should().Throw<ArgumentException>().WithParameterName("identifier");
        }

        [TestMethod]
        public void Quote_Hyphen_Throws()
        {
            Action act = () => PostgreSqlIdentifiers.Quote("mcp-tasks");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Quote_TooLong_Throws()
        {
            Action act = () => PostgreSqlIdentifiers.Quote(new string('a', 64));
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Quote_MaxLength_Succeeds()
        {
            var name = new string('A', 63);
            PostgreSqlIdentifiers.Quote(name).Should().Be("\"" + name + "\"");
        }

        [TestMethod]
        public void Validate_SimpleName_DoesNotThrow()
        {
            Action act = () => PostgreSqlIdentifiers.Validate("mcp_essentials_tasks");
            act.Should().NotThrow();
        }

        #endregion

    }

}
