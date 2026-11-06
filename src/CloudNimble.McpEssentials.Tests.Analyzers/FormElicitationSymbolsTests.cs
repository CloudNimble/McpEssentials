using CloudNimble.Breakdance.Extensions.MSTest2;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Analyzers
{

    /// <summary>
    /// Direct coverage for analyzer symbol helpers that compilation tests do not reach.
    /// </summary>
    [TestClass]
    public class FormElicitationSymbolsTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void IsFormElicitation_WhenTypeIsNull_IsFalse()
        {
            FormElicitationSymbols.IsFormElicitation(null).Should().BeFalse();
        }

        [TestMethod]
        public void IsFormBuilder_WhenTypeIsNull_IsFalse()
        {
            FormElicitationSymbols.IsFormBuilder(null).Should().BeFalse();
        }

        [TestMethod]
        public void WireName_EmptyAndAlreadyCamel_RoundTrips()
        {
            FormElicitationTypeMapper.WireName("").Should().Be("");
            FormElicitationTypeMapper.WireName("email").Should().Be("email");
            FormElicitationTypeMapper.WireName("Email").Should().Be("email");
        }

        [TestMethod]
        public async Task From_StaticImport_BindsTypeArgumentLocation()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                using static CloudNimble.McpEssentials.Core.Tasks.Elicitations.FormElicitationBuilder;

                public class SignupForm { public string Name { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        From<SignupForm>("Sign up");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task FormElicitationBuilder_QualifiedName_AnalyzesType()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class SignupForm { public string Name { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        _ = CloudNimble.McpEssentials.Core.Tasks.Elicitations.FormElicitationBuilder<SignupForm>.From("m");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task From_BuiltInArrayAndDelegate_Warns()
        {
            var array = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<int[]>("m");
                    }
                }
                """);
            array.Should().Contain(d => d.Id == "MCPESS005");

            var del = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<System.Action>("m");
                    }
                }
                """);
            del.Should().Contain(d => d.Id == "MCPESS005");
        }

        [TestMethod]
        public async Task Exclude_ParenthesizedAndBlockLambda()
        {
            var parenthesized = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Form { public string Name { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m").Exclude((x => x.Name));
                    }
                }
                """);
            parenthesized.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Exclude_FieldAccess_IsInvalidExpression()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Form { public string Name; }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m").Exclude(x => x.Name);
                    }
                }
                """);

            diags.Should().Contain(d => d.Id == "MCPESS003");
        }

        [TestMethod]
        public async Task From_HidesBaseProperty_AndWriteOnlyAndIndexer()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class BaseForm
                {
                    public string Name { get; set; }
                }

                public class DerivedForm : BaseForm
                {
                    public new string Name { get; set; }
                    public string WriteOnly { set { } }
                    public string this[int i] { get => ""; set { } }
                    public static string StaticName { get; set; }
                }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<DerivedForm>("m");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task From_EnumArray_IsMappable()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public enum Color { Red, Blue }
                public class Form { public Color[] Colors { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task LooksLikeLambda_DoubleParentheses()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Form { public string Name { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m").Exclude(((x => x.Name)));
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task From_OtherTypeNamedFrom_IsIgnored()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public static class Other
                {
                    public static void From<T>(string m) { }
                }

                public class Use
                {
                    public void Go()
                    {
                        Other.From<int>("m");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task NewUntypedBuilder_DoesNotCrash()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Use
                {
                    public void Go()
                    {
                        _ = FormElicitationBuilder.Create("m");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task From_NullableAndEnumList_AreMappable()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public enum Color { Red, Blue }
                public class Form
                {
                    public int? Age { get; set; }
                    public System.Collections.Generic.List<Color> Colors { get; set; }
                    public System.Collections.Generic.IEnumerable<Color> Palette { get; set; }
                }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Exclude_ParenthesizedProperty()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Form { public string Name { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m").Exclude(x => (x.Name));
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Exclude_CastProperty_IsSimpleAccess()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Form { public string Name { get; set; } }

                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.From<Form>("m").Exclude(x => (string)x.Name);
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task StringNamedLocalFunction_IsIgnored()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                public class Form { public string Name { get; set; } }

                public class Use
                {
                    public void String(System.Linq.Expressions.Expression<System.Func<Form, string>> expr) { }

                    public void Go()
                    {
                        String(x => x.Name);
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task NestedNamespaceFormElicitation_IsIgnored()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                namespace Elicitations
                {
                    public static class FormElicitationBuilder
                    {
                        public static void From<T>(string m) { }
                    }
                }

                namespace Tasks.Elicitations
                {
                    public static class FormElicitationBuilder
                    {
                        public static void From<T>(string m) { }
                    }
                }

                namespace Core.Tasks.Elicitations
                {
                    public static class FormElicitationBuilder
                    {
                        public static void From<T>(string m) { }
                    }
                }

                namespace McpEssentials.Core.Tasks.Elicitations
                {
                    public static class FormElicitationBuilder
                    {
                        public static void From<T>(string m) { }
                    }
                }

                namespace Other.CloudNimble.McpEssentials.Core.Tasks.Elicitations
                {
                    public static class FormElicitationBuilder
                    {
                        public static void From<T>(string m) { }
                    }
                }

                public class Use
                {
                    public void Go()
                    {
                        Elicitations.FormElicitationBuilder.From<int>("m");
                        Tasks.Elicitations.FormElicitationBuilder.From<int>("m");
                        Core.Tasks.Elicitations.FormElicitationBuilder.From<int>("m");
                        McpEssentials.Core.Tasks.Elicitations.FormElicitationBuilder.From<int>("m");
                        Other.CloudNimble.McpEssentials.Core.Tasks.Elicitations.FormElicitationBuilder.From<int>("m");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task OtherNamespaceFormElicitationBuilder_IsIgnored()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                namespace Other
                {
                    public class FormElicitationBuilder<T>
                    {
                        public FormElicitationBuilder(string m) { }
                    }
                }

                public class Use
                {
                    public void Go()
                    {
                        _ = new Other.FormElicitationBuilder<int>("m");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public async Task FormElicitationInWrongNamespace_IsIgnored()
        {
            var diags = await AnalyzerCompilation.GetDiagnosticsAsync("""
                namespace CloudNimble
                {
                    public static class FormElicitationBuilder
                    {
                        public static void From<T>(string m) { }
                    }
                }

                public class Use
                {
                    public void Go()
                    {
                        CloudNimble.FormElicitationBuilder.From<int>("m");
                    }
                }
                """);

            diags.Should().BeEmpty();
        }

        [TestMethod]
        public void IsPropertyExpressionMethod_ZeroParameters_IsFalse()
        {
            var compilation = AnalyzerCompilation.Compile("""
                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.Create("m").Build();
                    }
                }
                """);
            var type = compilation.GetTypeByMetadataName("CloudNimble.McpEssentials.Core.Tasks.Elicitations.FormElicitationBuilderBase");
            var build = type!.GetMembers("Build").OfType<Microsoft.CodeAnalysis.IMethodSymbol>().First(m => m.Parameters.Length == 0);
            FormElicitationSymbols.IsPropertyExpressionMethod(build).Should().BeFalse();
        }

        [TestMethod]
        public void IsMappable_IEnumerableOfEnum_IsTrue()
        {
            var compilation = AnalyzerCompilation.Compile("""
                public enum Color { Red, Blue }
                public class Form { public System.Collections.Generic.IEnumerable<Color> Colors { get; set; } }
                """);
            var form = compilation.GetTypeByMetadataName("Form");
            var property = form!.GetMembers("Colors").OfType<Microsoft.CodeAnalysis.IPropertySymbol>().Single();
            property.Type.TypeKind.Should().NotBe(Microsoft.CodeAnalysis.TypeKind.Error);
            FormElicitationTypeMapper.IsMappable(property.Type).Should().BeTrue();
            FormElicitationTypeMapper.TryGetEnumEnumerable(property.Type).Should().BeTrue();
        }

        [TestMethod]
        public void TryGetEnumEnumerable_IEnumerableOfInt_IsFalse()
        {
            var compilation = AnalyzerCompilation.Compile("""
                public class Form { public System.Collections.Generic.List<int> Numbers { get; set; } }
                """);
            var form = compilation.GetTypeByMetadataName("Form");
            var property = form!.GetMembers("Numbers").OfType<Microsoft.CodeAnalysis.IPropertySymbol>().Single();
            FormElicitationTypeMapper.TryGetEnumEnumerable(property.Type).Should().BeFalse();
        }

        [TestMethod]
        public void GetTypeArgumentLocation_WhenNotMemberOrGeneric_ReturnsNull()
        {
            var name = SyntaxFactory.IdentifierName("From");
            FormElicitationFromTypeAnalyzer.GetTypeArgumentLocation(name).Should().BeNull();
        }

        [TestMethod]
        public void IsSealingAndFieldAddingHelpers_MatchBuilderMethods()
        {
            var compilation = AnalyzerCompilation.Compile("""
                public class Use
                {
                    public void Go()
                    {
                        FormElicitationBuilder.Create("m").String("a").Build();
                    }
                }
                """);
            var type = compilation.GetTypeByMetadataName("CloudNimble.McpEssentials.Core.Tasks.Elicitations.FormElicitationBuilderBase");
            var build = type!.GetMembers("Build").OfType<IMethodSymbol>().First(m => m.Parameters.Length == 0);
            var toXml = type.GetMembers("ToXml").OfType<IMethodSymbol>().First();
            var stringMethod = type.GetMembers("String").OfType<IMethodSymbol>().First(m => m.Parameters.Length == 1);

            FormElicitationSymbols.IsSealingMethod(build).Should().BeTrue();
            FormElicitationSymbols.IsSealingMethod(toXml).Should().BeTrue();
            FormElicitationSymbols.IsSealingMethod(stringMethod).Should().BeFalse();
            FormElicitationSymbols.IsFieldAddingMethod(stringMethod).Should().BeTrue();
            FormElicitationSymbols.IsFieldAddingMethod(build).Should().BeFalse();
            FormElicitationSymbols.IsSealingName("ToJson").Should().BeTrue();
            FormElicitationSymbols.IsSealingName("Title").Should().BeFalse();
            FormElicitationSymbols.IsFieldAddingName("Exclude").Should().BeTrue();
            FormElicitationSymbols.IsFieldAddingName("Build").Should().BeFalse();
        }

        [TestMethod]
        public void GetInvokedName_WhenNotMemberOrSimple_ReturnsNull()
        {
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class C { void M() { ((System.Action)(() => {}))(); } }");
            var invocation = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>().Last();
            FormElicitationSymbols.GetInvokedName(invocation).Should().BeNull();
        }

        #endregion

    }

}
