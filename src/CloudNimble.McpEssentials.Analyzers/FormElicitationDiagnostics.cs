using Microsoft.CodeAnalysis;

namespace CloudNimble.McpEssentials.Analyzers
{

    /// <summary>
    /// Diagnostic descriptors for form elicitation authoring mistakes.
    /// </summary>
    internal static class FormElicitationDiagnostics
    {

        #region Fields

        /// <summary>
        /// Help link shared by every form-elicitation diagnostic.
        /// </summary>
        public const string HelpLink = "https://easyaf.dev/mcpessentials/guides/analyzers";

        /// <summary>
        /// Diagnostic category shown in the IDE.
        /// </summary>
        public const string Category = "Usage";

        #endregion

        #region Properties

        /// <summary>
        /// A public property on <c>From&lt;T&gt;</c> that the conventions skip.
        /// </summary>
        public static readonly DiagnosticDescriptor UnmappableProperty = new(
            id: "MCPESS001",
            title: "Form type has a property the elicitation schema cannot represent",
            messageFormat: "Property '{0}' on '{1}' is skipped by FormElicitation.From. Exclude it or change the type to a supported primitive.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        /// <summary>
        /// Two properties collapse to the same camelCase wire name.
        /// </summary>
        public static readonly DiagnosticDescriptor WireNameCollision = new(
            id: "MCPESS002",
            title: "Form properties collide on the same camelCase wire name",
            messageFormat: "Properties '{0}' and '{1}' on '{2}' both map to form field '{3}'. Rename one or Exclude one.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        /// <summary>
        /// Expression is not a simple property access on the form type.
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidPropertyExpression = new(
            id: "MCPESS003",
            title: "Form expression must be a simple property access",
            messageFormat: "Expression must be a simple property access on the form type, such as x => x.Email",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        /// <summary>
        /// Exclude of a member that was never going to become a field.
        /// </summary>
        public static readonly DiagnosticDescriptor ExcludeUnmappable = new(
            id: "MCPESS004",
            title: "Exclude of an unmappable property is a no-op",
            messageFormat: "Exclude of '{0}' does nothing because the property is not a supported form field",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        /// <summary>
        /// From is used on a built-in or enum instead of a form DTO.
        /// </summary>
        public static readonly DiagnosticDescriptor NotAFormType = new(
            id: "MCPESS005",
            title: "From expects a form type of properties",
            messageFormat: "'{0}' is not a form type. FormElicitation.From expects a class or struct of public properties.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        /// <summary>
        /// A field starter runs after the form was materialized.
        /// </summary>
        public static readonly DiagnosticDescriptor MutationAfterBuild = new(
            id: "MCPESS006",
            title: "Do not add form fields after the form is built",
            messageFormat: "'{0}' adds a field after the form was materialized. Build, ToXml, ToJson, ToDefinition, and ToInputRequest seal the form.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        #endregion

    }

}
