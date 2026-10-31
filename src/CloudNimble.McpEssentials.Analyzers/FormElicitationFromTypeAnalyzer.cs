using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CloudNimble.McpEssentials.Analyzers
{

    /// <summary>
    /// Warns when <c>FormElicitation.From&lt;T&gt;</c> or <c>new FormElicitationBuilder&lt;T&gt;</c>
    /// would skip properties, collide on a wire name, or use a built-in as T.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FormElicitationFromTypeAnalyzer : DiagnosticAnalyzer
    {

        #region Properties

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            [FormElicitationDiagnostics.UnmappableProperty,
             FormElicitationDiagnostics.WireNameCollision,
             FormElicitationDiagnostics.NotAFormType];

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Analyzes <c>FormElicitation.From&lt;T&gt;(...)</c>.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!FormElicitationSymbols.IsFromInvocationName(invocation))
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
                || method.Name != "From"
                || !FormElicitationSymbols.IsFormElicitation(method.ContainingType)
                || method.TypeArguments.Length != 1)
            {
                return;
            }

            var typeArgument = method.TypeArguments[0];
            var location = GetTypeArgumentLocation(invocation.Expression) ?? invocation.GetLocation();
            AnalyzeFormType(context, typeArgument, location, invocation);
        }

        /// <summary>
        /// Analyzes <c>new FormElicitationBuilder&lt;T&gt;(...)</c>.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            if (creation.Type is not GenericNameSyntax { Identifier.ValueText: "FormElicitationBuilder" }
                and not QualifiedNameSyntax { Right: GenericNameSyntax { Identifier.ValueText: "FormElicitationBuilder" } })
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol is not IMethodSymbol ctor
                || ctor.ContainingType is not { } containing
                || containing.OriginalDefinition.Name != "FormElicitationBuilder"
                || !FormElicitationSymbols.IsFormBuilder(containing)
                || containing.TypeArguments.Length != 1)
            {
                return;
            }

            var location = creation.Type.GetLocation();
            AnalyzeFormType(context, containing.TypeArguments[0], location, creation);
        }

        /// <summary>
        /// Reports not-a-form-type, collisions, and skipped properties for <paramref name="formType"/>.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        /// <param name="formType">The type argument.</param>
        /// <param name="location">Where to report.</param>
        /// <param name="seedNode">The From invocation or constructor, used to find chained Exclude calls.</param>
        private static void AnalyzeFormType(
            SyntaxNodeAnalysisContext context,
            ITypeSymbol formType,
            Location location,
            SyntaxNode seedNode)
        {
            var typeName = formType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            if (FormElicitationTypeMapper.IsBuiltInFormType(formType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    FormElicitationDiagnostics.NotAFormType,
                    location,
                    typeName));
                return;
            }

            var properties = FormElicitationTypeMapper.GetPublicInstanceProperties(formType);
            var excluded = CollectExcludedPropertyNames(seedNode, context.SemanticModel);
            var byWire = new Dictionary<string, IPropertySymbol>();

            foreach (var property in properties)
            {
                if (excluded is not null && excluded.Contains(property.Name))
                {
                    continue;
                }

                var wire = FormElicitationTypeMapper.WireName(property.Name);
                if (byWire.TryGetValue(wire, out var existing))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FormElicitationDiagnostics.WireNameCollision,
                        location,
                        existing.Name,
                        property.Name,
                        typeName,
                        wire));
                    continue;
                }

                if (FormElicitationTypeMapper.IsMappable(property.Type))
                {
                    byWire[wire] = property;
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    FormElicitationDiagnostics.UnmappableProperty,
                    location,
                    property.Name,
                    typeName));
            }
        }

        /// <summary>
        /// Collects property names passed to <c>Exclude</c> on the fluent chain that starts at <paramref name="seedNode"/>.
        /// </summary>
        /// <param name="seedNode">The From invocation or constructor.</param>
        /// <param name="model">The semantic model.</param>
        /// <returns>Excluded property names.</returns>
        private static HashSet<string>? CollectExcludedPropertyNames(SyntaxNode seedNode, SemanticModel model)
        {
            HashSet<string>? excluded = null;
            SyntaxNode current = seedNode;
            while (current.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } member)
            {
                current = invocation;
                if (member.Name.Identifier.ValueText != "Exclude" || invocation.ArgumentList.Arguments.Count == 0)
                {
                    continue;
                }

                var argument = invocation.ArgumentList.Arguments[0].Expression;
                if (FormElicitationPropertyAccess.TryGetSimpleProperty(argument, model, out var property, out _)
                    && property is not null)
                {
                    excluded ??= [];
                    excluded.Add(property.Name);
                }
            }

            return excluded;
        }

        /// <summary>
        /// Gets the source span of the generic type argument on <c>From&lt;T&gt;</c>.
        /// </summary>
        /// <param name="expression">The invocation expression (usually a member access).</param>
        /// <returns>The type argument location, or <see langword="null"/>.</returns>
        private static Location? GetTypeArgumentLocation(ExpressionSyntax expression)
        {
            SimpleNameSyntax? name = expression switch
            {
                MemberAccessExpressionSyntax member => member.Name,
                GenericNameSyntax generic => generic,
                _ => null
            };

            if (name is GenericNameSyntax { TypeArgumentList.Arguments.Count: > 0 } withArguments)
            {
                return withArguments.TypeArgumentList.Arguments[0].GetLocation();
            }

            return null;
        }

        #endregion

    }

}
