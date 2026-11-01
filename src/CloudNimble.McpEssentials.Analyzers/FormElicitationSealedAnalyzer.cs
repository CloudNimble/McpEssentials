using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace CloudNimble.McpEssentials.Analyzers
{

    /// <summary>
    /// Warns when a field starter or <c>Exclude</c> runs after the same form was materialized.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FormElicitationSealedAnalyzer : DiagnosticAnalyzer
    {

        #region Properties

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            [FormElicitationDiagnostics.MutationAfterBuild];

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeBody, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeBody, SyntaxKind.ConstructorDeclaration);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Walks invocations in source order and flags field adds after a sealing call on the same local.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        private static void AnalyzeBody(SyntaxNodeAnalysisContext context)
        {
            var sealedNames = new HashSet<string>();
            foreach (var invocation in context.Node.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var name = FormElicitationSymbols.GetInvokedName(invocation)?.Identifier.ValueText;
                if (name is null)
                {
                    continue;
                }

                var isSeal = FormElicitationSymbols.IsSealingName(name);
                var isAdd = FormElicitationSymbols.IsFieldAddingName(name);
                if (!isSeal && !isAdd)
                {
                    continue;
                }

                if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
                    || isSeal && !FormElicitationSymbols.IsSealingMethod(method)
                    || isAdd && !FormElicitationSymbols.IsFieldAddingMethod(method))
                {
                    continue;
                }

                var root = GetRootIdentifier(invocation, context.SemanticModel, context.CancellationToken);
                if (root is null)
                {
                    continue;
                }

                if (isSeal)
                {
                    sealedNames.Add(root);
                    continue;
                }

                if (sealedNames.Contains(root))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FormElicitationDiagnostics.MutationAfterBuild,
                        invocation.GetLocation(),
                        method.Name));
                }
            }
        }

        /// <summary>
        /// Gets the leftmost identifier of a fluent chain, including <c>this</c>.
        /// </summary>
        /// <param name="invocation">The invocation.</param>
        /// <param name="semanticModel">The semantic model for the tree.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The identifier text, or <see langword="null"/> when the receiver is not a local or this.</returns>
        private static string? GetRootIdentifier(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            ExpressionSyntax expression = invocation.Parent is ConditionalAccessExpressionSyntax conditional
                ? conditional.Expression
                : invocation.Expression;

            while (true)
            {
                switch (expression)
                {
                    case InvocationExpressionSyntax nested:
                        expression = nested.Parent is ConditionalAccessExpressionSyntax nestedConditional
                            ? nestedConditional.Expression
                            : nested.Expression;
                        continue;
                    case MemberAccessExpressionSyntax member:
                        expression = member.Expression;
                        continue;
                    case ConditionalAccessExpressionSyntax access:
                        expression = access.Expression;
                        continue;
                    case ParenthesizedExpressionSyntax parenthesized:
                        expression = parenthesized.Expression;
                        continue;
                    case IdentifierNameSyntax identifier:
                        var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
                        if (symbol is ITypeSymbol or IMethodSymbol { IsStatic: true })
                        {
                            return null;
                        }

                        return symbol is IMethodSymbol ? "this" : identifier.Identifier.ValueText;
                    case ThisExpressionSyntax:
                        return "this";
                    default:
                        return null;
                }
            }
        }

        #endregion

    }

}
