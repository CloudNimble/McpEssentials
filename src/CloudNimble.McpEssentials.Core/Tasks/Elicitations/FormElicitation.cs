using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Starts a fluent MCP form elicitation. <see cref="Create"/> is untyped;
    /// <see cref="From{T}"/> seeds fields from a CLR type, then the same chain customizes them.
    /// <see cref="Bind{T}"/> projects an accepted result onto that type.
    /// </summary>
    public static class FormElicitation
    {

        #region Public Methods

        /// <summary>
        /// Starts an empty form with <paramref name="message"/> as the prompt.
        /// </summary>
        /// <param name="message">The message presented to the user.</param>
        /// <returns>An untyped form builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
        public static FormElicitationBuilder Create(string message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return new FormElicitationBuilder(message);
        }

        /// <summary>
        /// Starts a form seeded from the public instance properties of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The form type. Unmappable properties are skipped.</typeparam>
        /// <param name="message">The message presented to the user.</param>
        /// <returns>A typed form builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
        public static FormElicitationBuilder<T> From<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return new FormElicitationBuilder<T>(message);
        }

        /// <summary>
        /// Projects accepted form content onto <typeparamref name="T"/>. Cancel and decline return default content.
        /// </summary>
        /// <typeparam name="T">The form type. Property names match camelCase wire names.</typeparam>
        /// <param name="result">The untyped elicitation result.</param>
        /// <param name="options">
        /// Serializer options. When omitted, properties are camelCase and enums use their member names.
        /// Native AOT callers should pass options from a <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>.
        /// </param>
        /// <returns>A typed result with the same action as <paramref name="result"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [RequiresDynamicCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Binder.Bind is the annotated implementation.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Binder.Bind is the annotated implementation.")]
        public static ElicitResult<T> Bind<T>(ElicitResult result, JsonSerializerOptions? options = null) =>
            FormElicitationBinder.Bind<T>(result, options);

        #endregion

    }

}
