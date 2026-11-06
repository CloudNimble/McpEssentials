using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CloudNimble.McpEssentials.Analyzers
{

    /// <summary>
    /// Well-known names for the Core form elicitation API.
    /// </summary>
    internal static class FormElicitationSymbols
    {

        #region Fields

        /// <summary>
        /// Namespace of the fluent form types.
        /// </summary>
        public const string Namespace = "CloudNimble.McpEssentials.Core.Tasks.Elicitations";

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets whether <paramref name="type"/> is the non-generic <c>FormElicitationBuilder</c>.
        /// </summary>
        /// <param name="type">A type symbol.</param>
        /// <returns><see langword="true"/> for the untyped builder factory.</returns>
        public static bool IsFormElicitation(INamedTypeSymbol? type)
        {
            return type is { Name: "FormElicitationBuilder", IsGenericType: false } && IsElicitationsNamespace(type);
        }

        /// <summary>
        /// Gets whether <paramref name="type"/> is <c>FormElicitationBuilder</c> or a typed/untyped field facade.
        /// </summary>
        /// <param name="type">A type symbol.</param>
        /// <returns><see langword="true"/> for a fluent form type.</returns>
        public static bool IsFormBuilder(INamedTypeSymbol? type)
        {
            if (type is null || !IsElicitationsNamespace(type))
            {
                return false;
            }

            var name = type.OriginalDefinition.Name;
            return name is "FormElicitationBuilder"
                or "FormElicitationBuilderBase"
                or "StringFieldBuilder"
                or "NumberFieldBuilder"
                or "BooleanFieldBuilder"
                or "ChoiceFieldBuilder"
                or "MultiChoiceFieldBuilder";
        }

        /// <summary>
        /// Gets whether <paramref name="method"/> is a property-expression starter or <c>Exclude</c>.
        /// </summary>
        /// <param name="method">A method symbol.</param>
        /// <returns><see langword="true"/> when the first argument is expected to be a property lambda.</returns>
        public static bool IsPropertyExpressionMethod(IMethodSymbol method)
        {
            if (!IsFormBuilder(method.ContainingType) || method.Parameters.Length == 0)
            {
                return false;
            }

            return IsFieldAddingName(method.Name);
        }

        /// <summary>
        /// Gets whether <paramref name="method"/> materializes the form and seals further field changes.
        /// </summary>
        /// <param name="method">A method symbol.</param>
        /// <returns><see langword="true"/> for Build, ToXml, ToJson, ToDefinition, and ToInputRequest.</returns>
        public static bool IsSealingMethod(IMethodSymbol method)
        {
            if (!IsFormBuilder(method.ContainingType))
            {
                return false;
            }

            return method.Name is "Build" or "ToXml" or "ToJson" or "ToDefinition" or "ToInputRequest";
        }

        /// <summary>
        /// Gets whether <paramref name="method"/> adds or removes a field on a fluent form.
        /// </summary>
        /// <param name="method">A method symbol.</param>
        /// <returns><see langword="true"/> for starters and Exclude.</returns>
        public static bool IsFieldAddingMethod(IMethodSymbol method)
        {
            return IsFormBuilder(method.ContainingType) && IsFieldAddingName(method.Name);
        }

        /// <summary>
        /// Gets whether <paramref name="name"/> is a starter or <c>Exclude</c>.
        /// </summary>
        /// <param name="name">A method name.</param>
        /// <returns><see langword="true"/> for field-mutating fluent methods.</returns>
        public static bool IsFieldAddingName(string? name)
        {
            return name is "Exclude"
                or "String"
                or "Email"
                or "Uri"
                or "Date"
                or "DateTime"
                or "Integer"
                or "Number"
                or "Boolean"
                or "Choice"
                or "MultiChoice";
        }

        /// <summary>
        /// Gets whether <paramref name="name"/> is a materializing method.
        /// </summary>
        /// <param name="name">A method name.</param>
        /// <returns><see langword="true"/> for Build and the output methods.</returns>
        public static bool IsSealingName(string? name)
        {
            return name is "Build" or "ToXml" or "ToJson" or "ToDefinition" or "ToInputRequest";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets whether <paramref name="type"/> lives in the elicitation namespace.
        /// </summary>
        /// <param name="type">A type symbol.</param>
        /// <returns><see langword="true"/> for Core.Tasks.Elicitations.</returns>
        private static bool IsElicitationsNamespace(INamedTypeSymbol type)
        {
            var ns = type.ContainingNamespace;
            if (ns is null || ns.Name != "Elicitations")
            {
                return false;
            }

            ns = ns.ContainingNamespace;
            if (ns is null || ns.Name != "Tasks")
            {
                return false;
            }

            ns = ns.ContainingNamespace;
            if (ns is null || ns.Name != "Core")
            {
                return false;
            }

            ns = ns.ContainingNamespace;
            if (ns is null || ns.Name != "McpEssentials")
            {
                return false;
            }

            ns = ns.ContainingNamespace;
            if (ns is null || ns.Name != "CloudNimble")
            {
                return false;
            }

            ns = ns.ContainingNamespace;
            return ns is null || ns.IsGlobalNamespace;
        }

        /// <summary>
        /// Gets whether <paramref name="invocation"/> is a candidate <c>From</c> call before any semantic lookup.
        /// </summary>
        /// <param name="invocation">An invocation expression.</param>
        /// <returns><see langword="true"/> when the invoked name is the generic identifier <c>From</c>.</returns>
        public static bool IsFromInvocationName(InvocationExpressionSyntax invocation)
        {
            var name = GetInvokedName(invocation);
            return name is GenericNameSyntax { Identifier.ValueText: "From" }
                or IdentifierNameSyntax { Identifier.ValueText: "From" };
        }

        /// <summary>
        /// Gets whether <paramref name="invocation"/> is a candidate property-expression method before semantic lookup.
        /// </summary>
        /// <param name="invocation">An invocation expression.</param>
        /// <returns><see langword="true"/> when the method name is a starter or Exclude.</returns>
        public static bool IsPropertyExpressionInvocationName(InvocationExpressionSyntax invocation)
        {
            var name = GetInvokedName(invocation);
            var identifier = name?.Identifier.ValueText;
            return identifier is "Exclude"
                or "String"
                or "Email"
                or "Uri"
                or "Date"
                or "DateTime"
                or "Integer"
                or "Number"
                or "Boolean"
                or "Choice"
                or "MultiChoice";
        }

        /// <summary>
        /// Gets the simple or generic name of the invoked member.
        /// </summary>
        /// <param name="invocation">An invocation expression.</param>
        /// <returns>The name syntax, or <see langword="null"/>.</returns>
        public static SimpleNameSyntax? GetInvokedName(InvocationExpressionSyntax invocation)
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name,
                MemberBindingExpressionSyntax binding => binding.Name,
                SimpleNameSyntax simple => simple,
                _ => null
            };
        }

        #endregion

    }

}
