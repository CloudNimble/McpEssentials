using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CloudNimble.McpEssentials.Analyzers
{

    /// <summary>
    /// Flags nested or invalid property lambdas, and <c>Exclude</c> of a member that would never be seeded.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FormElicitationExpressionAnalyzer : DiagnosticAnalyzer
    {

        #region Properties

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            [FormElicitationDiagnostics.InvalidPropertyExpression,
             FormElicitationDiagnostics.ExcludeUnmappable];

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Analyzes fluent expression starters and <c>Exclude</c>.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation.ArgumentList.Arguments.Count == 0)
            {
                return;
            }

            var argument = invocation.ArgumentList.Arguments[0].Expression;
            if (!LooksLikeLambda(argument) || !FormElicitationSymbols.IsPropertyExpressionInvocationName(invocation))
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
                || !FormElicitationSymbols.IsPropertyExpressionMethod(method))
            {
                return;
            }

            if (!FormElicitationPropertyAccess.TryGetSimpleProperty(argument, context.SemanticModel, out var property, out var invalid)
                || property is null)
            {
                if (invalid)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FormElicitationDiagnostics.InvalidPropertyExpression,
                        argument.GetLocation()));
                }

                return;
            }

            if (method.Name == "Exclude" && !FormElicitationTypeMapper.IsMappable(property.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    FormElicitationDiagnostics.ExcludeUnmappable,
                    argument.GetLocation(),
                    property.Name));
            }
        }

        /// <summary>
        /// Gets whether <paramref name="expression"/> is (or wraps) a lambda, so string overloads are ignored.
        /// </summary>
        /// <param name="expression">The first argument.</param>
        /// <returns><see langword="true"/> for lambdas.</returns>
        private static bool LooksLikeLambda(ExpressionSyntax expression)
        {
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return LooksLikeLambda(parenthesized.Expression);
            }

            return expression is LambdaExpressionSyntax;
        }

        #endregion

    }

}
