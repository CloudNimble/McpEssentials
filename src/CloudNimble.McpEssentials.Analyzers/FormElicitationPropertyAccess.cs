using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CloudNimble.McpEssentials.Analyzers
{

    /// <summary>
    /// Resolves a lambda to a simple property access, matching Core <c>FormElicitationProperty.From</c>.
    /// </summary>
    internal static class FormElicitationPropertyAccess
    {

        #region Public Methods

        /// <summary>
        /// Gets the property referenced by a simple <c>x =&gt; x.Name</c> lambda.
        /// </summary>
        /// <param name="expression">The argument expression, usually a lambda.</param>
        /// <param name="model">The semantic model.</param>
        /// <param name="property">The property when the expression is simple access.</param>
        /// <param name="isNestedOrInvalid">
        /// <see langword="true"/> when the expression is present but is not a simple property access.
        /// </param>
        /// <returns><see langword="true"/> when a property was resolved.</returns>
        public static bool TryGetSimpleProperty(
            ExpressionSyntax expression,
            SemanticModel model,
            out IPropertySymbol? property,
            out bool isNestedOrInvalid)
        {
            property = null;
            isNestedOrInvalid = false;

            var body = UnwrapLambda(expression);
            if (body is null)
            {
                isNestedOrInvalid = true;
                return false;
            }

            if (body is CastExpressionSyntax cast)
            {
                body = cast.Expression;
            }

            if (body is ParenthesizedExpressionSyntax parenthesized)
            {
                body = parenthesized.Expression;
            }

            if (body is not MemberAccessExpressionSyntax member
                || member.Expression is not IdentifierNameSyntax)
            {
                isNestedOrInvalid = true;
                return false;
            }

            var symbol = model.GetSymbolInfo(member).Symbol;
            if (symbol is IPropertySymbol resolved)
            {
                property = resolved;
                return true;
            }

            isNestedOrInvalid = true;
            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Unwraps a lambda or parenthesized lambda to its body expression.
        /// </summary>
        /// <param name="expression">The argument.</param>
        /// <returns>The body, or <see langword="null"/>.</returns>
        private static ExpressionSyntax? UnwrapLambda(ExpressionSyntax expression)
        {
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return UnwrapLambda(parenthesized.Expression);
            }

            if (expression is LambdaExpressionSyntax lambda)
            {
                return lambda.Body as ExpressionSyntax;
            }

            return expression;
        }

        #endregion

    }

}
