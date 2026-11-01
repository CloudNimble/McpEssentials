using CloudNimble.Breakdance.Extensions.MSTest2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Analyzers
{

    /// <summary>
    /// Coverage for MCPESS006 when a field is added after the form is materialized.
    /// </summary>
    [TestClass]
    public class FormElicitationSealedAnalyzerTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public async Task StringAfterBuild_WarnsMcpess006()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        var form = FormElicitation.Create("m").String("a");
                        form.Build();
                        form.Integer("b");
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task ToXmlThenEmail_WarnsMcpess006()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        var form = FormElicitation.Create("m");
                        form.ToXml();
                        form.Email("e");
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task ChainEndsWithBuild_DoesNotWarn()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        FormElicitation.Create("m").String("a").Build();
                    }
                }
                """);

            diags.Should().NotContain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task DifferentLocals_DoesNotWarn()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        var a = FormElicitation.Create("m");
                        a.Build();
                        var b = FormElicitation.Create("n");
                        b.String("x");
                    }
                }
                """);

            diags.Should().NotContain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task LocalFunction_WarnsMcpess006()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        void Inner()
                        {
                            var form = FormElicitation.Create("m");
                            form.ToInputRequest();
                            form.String("a");
                        }
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task NestedLambda_DoesNotWarnOuter()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        var form = FormElicitation.Create("m");
                        form.Build();
                        System.Action inner = () =>
                        {
                            var other = FormElicitation.Create("n");
                            other.String("a");
                        };
                    }
                }
                """);

            diags.Should().NotContain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task UnrelatedBuildMethod_DoesNotWarn()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Build() { }

                    public void Go()
                    {
                        Build();
                        var form = FormElicitation.Create("m");
                        form.String("a");
                    }
                }
                """);

            diags.Should().NotContain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task ThisBuildThenString_WarnsMcpess006()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use : FormElicitationBuilder
                {
                    public Use() : base("m") { }

                    public void Go()
                    {
                        this.Build();
                        this.String("a");
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task DelegateNamedBuild_DoesNotWarn()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        System.Action Build = () => { };
                        Build();
                        ((System.Action)(() => { }))();
                        var form = FormElicitation.Create("m");
                        form.String("a");
                    }
                }
                """);

            diags.Should().NotContain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task ConditionalThenBuild_WarnsMcpess006()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        var form = FormElicitation.Create("m");
                        (form?.String("a")).Build();
                        form.Integer("b");
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task ParenthesizedReceiver_WarnsMcpess006()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        var form = FormElicitation.Create("m");
                        (form).Build();
                        form.String("a");
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task ConstructorAndToDefinition_WarnsMcpess006()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public Use()
                    {
                        var form = FormElicitation.Create("m");
                        form.ToDefinition();
                        form.String("a");
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task ObjectCreationChain_DoesNotWarn()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        new FormElicitationBuilder("m").String("a").Build();
                        new FormElicitationBuilder("m").String("b");
                    }
                }
                """);

            diags.Should().NotContain(d => d.Id == "MCPESS006");
        }

        [TestMethod]
        public async Task ConditionalAccess_WarnsMcpess006()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder form = FormElicitation.Create("m");
                        form?.ToJson();
                        form.String("a");
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS006");
        }

        #endregion

    }

}
