using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Projects an untyped <see cref="ElicitResult"/> onto a CLR form type.
    /// Wire names are camelCase; enum members match <see cref="Enum.GetNames"/>.
    /// </summary>
    internal static class FormElicitationBinder
    {

        #region Fields

        internal static readonly JsonSerializerOptions DefaultOptions = McpJsonUtilities.DefaultOptions;

        #endregion

        #region Public Methods

        /// <summary>
        /// Deserializes accepted <see cref="ElicitResult.Content"/> as <typeparamref name="T"/>.
        /// Cancel and decline leave <see cref="ElicitResult{T}.Content"/> at its default.
        /// </summary>
        /// <typeparam name="T">The form type.</typeparam>
        /// <param name="result">The untyped elicitation result.</param>
        /// <param name="options">Serializer options. Defaults to camelCase properties and string enums.</param>
        /// <returns>A typed result with the same <see cref="ElicitResult.Action"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [RequiresDynamicCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Callers that need AOT pass source-generated JsonSerializerOptions.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Callers that need AOT pass source-generated JsonSerializerOptions.")]
        public static ElicitResult<T> ToTyped<T>(ElicitResult result, JsonSerializerOptions? options)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (!result.IsAccepted || result.Content is null)
            {
                return new ElicitResult<T> { Action = result.Action, Content = default };
            }

            var serializerOptions = options ?? DefaultOptions;
            var utf8 = JsonSerializer.SerializeToUtf8Bytes(result.Content, serializerOptions);
            var typed = JsonSerializer.Deserialize<T>(utf8, serializerOptions);
            return new ElicitResult<T> { Action = result.Action, Content = typed };
        }

        #endregion

    }

}
