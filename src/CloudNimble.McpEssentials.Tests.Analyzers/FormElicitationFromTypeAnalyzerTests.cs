using CloudNimble.Breakdance.Extensions.MSTest2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Analyzers
{

    /// <summary>
    /// Coverage for MCPESS001, MCPESS002, and MCPESS005 on <c>From&lt;T&gt;</c>.
    /// </summary>
    [TestClass]
    public class FormElicitationFromTypeAnalyzerTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public async Task From_MappableType_NoDiagnostics()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class SignupForm
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitation.From<SignupForm>("Sign up");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task From_UnmappableProperty_WarnsMcpess001()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Address
                {
                    public string City { get; set; }
                }

                public class UnmappableForm
                {
                    public string Name { get; set; }
                    public Address Home { get; set; }
                }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitation.From<UnmappableForm>("m");
                    }
                }
                """);

            diags.Should().ContainSingle(d => d.Id == "MCPESS001");
            diags[0].GetMessage().Should().Contain("Home");
        }

        [TestMethod]
        public async Task From_ExcludeUnmappable_SuppressesMcpess001()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Address
                {
                    public string City { get; set; }
                }

                public class UnmappableForm
                {
                    public string Name { get; set; }
                    public Address Home { get; set; }
                }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitation.From<UnmappableForm>("m").Exclude(x => x.Home);
                    }
                }
                """);

            diags.Should().NotContain(d => d.Id == "MCPESS001");
        }

        [TestMethod]
        public async Task From_WireNameCollision_ErrorsMcpess002()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class CollisionForm
                {
                    public string EmailAddress { get; set; }
                    public string emailAddress { get; set; }
                }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitation.From<CollisionForm>("m");
                    }
                }
                """);

            diags.Should().ContainSingle(d => d.Id == "MCPESS002");
        }

        [TestMethod]
        public async Task From_String_WarnsMcpess005()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        FormElicitation.From<string>("m");
                    }
                }
                """);

            diags.Should().ContainSingle(d => d.Id == "MCPESS005");
        }

        [TestMethod]
        public async Task From_EmptyClass_NoDiagnostics()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class EmptyForm
                {
                }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitation.From<EmptyForm>("Confirm?");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Constructor_Unmappable_WarnsMcpess001()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Address { public string City { get; set; } }
                public class Bag { public Address Home { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        new FormElicitationBuilder<Bag>("m");
                    }
                }
                """);

            diags.Should().ContainSingle(d => d.Id == "MCPESS001");
        }

        #endregion

    }

}
