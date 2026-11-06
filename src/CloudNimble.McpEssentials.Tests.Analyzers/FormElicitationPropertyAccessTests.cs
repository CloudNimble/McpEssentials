using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Analyzers;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.McpEssentials.Tests.Analyzers
{

    /// <summary>
    /// Syntax-only coverage for lambda unwrapping.
    /// </summary>
    [TestClass]
    public class FormElicitationPropertyAccessTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void TryGetSimpleProperty_BlockLambda_IsInvalid()
        {
            var lambda = SyntaxFactory.SimpleLambdaExpression(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
                SyntaxFactory.Block());

            FormElicitationPropertyAccess.TryGetSimpleProperty(lambda, null!, out _, out var invalid)
                .Should().BeFalse();
            invalid.Should().BeTrue();
        }

        [TestMethod]
        public void TryGetSimpleProperty_Identifier_IsInvalid()
        {
            var name = SyntaxFactory.IdentifierName("foo");

            FormElicitationPropertyAccess.TryGetSimpleProperty(name, null!, out _, out var invalid)
                .Should().BeFalse();
            invalid.Should().BeTrue();
        }

        [TestMethod]
        public void TryGetSimpleProperty_CastIdentifier_IsInvalid()
        {
            var cast = SyntaxFactory.CastExpression(
                SyntaxFactory.ParseTypeName("string"),
                SyntaxFactory.IdentifierName("x"));

            FormElicitationPropertyAccess.TryGetSimpleProperty(cast, null!, out _, out var invalid)
                .Should().BeFalse();
            invalid.Should().BeTrue();
        }

        #endregion

    }

}
