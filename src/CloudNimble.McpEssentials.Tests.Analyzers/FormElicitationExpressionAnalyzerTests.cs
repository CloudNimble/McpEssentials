using CloudNimble.Breakdance.Extensions.MSTest2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Analyzers
{

    /// <summary>
    /// Coverage for MCPESS003 and MCPESS004 on expression starters and Exclude.
    /// </summary>
    [TestClass]
    public class FormElicitationExpressionAnalyzerTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public async Task NestedExpression_ErrorsMcpess003()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Address { public string City { get; set; } }
                public class Form { public Address Home { get; set; } public string Name { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m").Exclude(x => x.Home.City);
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS003");
        }

        [TestMethod]
        public async Task ExcludeUnmappable_InfosMcpess004()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Address { public string City { get; set; } }
                public class Form { public Address Home { get; set; } public string Name { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m").Exclude(x => x.Home);
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS004");
            diags.Should().NotContain(d => d.Id == "MCPESS001");
        }

        [TestMethod]
        public async Task ExcludeMappable_NoMcpess004()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Form { public string Name { get; set; } public string Email { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m").Exclude(x => x.Email);
                    }
                }
                """);

            diags.Should().NotContain(d => d.Id == "MCPESS004");
            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task StringNamedOverload_NoDiagnostic()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.Create("m").String("filter").Integer("top");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task MethodCallExpression_ErrorsMcpess003()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Form { public string Name { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m").String(x => x.Name.Trim());
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS003");
        }

        #endregion

    }

}
