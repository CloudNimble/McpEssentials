using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server
{

    /// <summary>
    /// Sends a fluent form through <see cref="McpServer.ElicitAsync(ElicitRequestParams, CancellationToken)"/>.
    /// When the call runs inside an MCP task, the SDK interceptor parks the form on the task automatically.
    /// </summary>
    public static class McpEssentials_Core_McpServerExtensions
    {

        #region Public Methods

        /// <summary>
        /// Builds <paramref name="form"/> and elicits it from the client.
        /// </summary>
        /// <param name="server">The MCP server.</param>
        /// <param name="form">A form builder or field facade.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The untyped elicitation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="server"/> or <paramref name="form"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// Metadata-driven tools keep the builder and add a field per property name:
        /// <code>
        /// var form = FormElicitation.Create($"How should we query {entitySet}?");
        /// form.String("filter").Title("$filter");
        /// form.Integer("top").Title("$top").Min(1).Max(1000).Default(20);
        /// var result = await server.ElicitAsync(form, cancellationToken);
        /// </code>
        /// </example>
        public static ValueTask<ElicitResult> ElicitAsync(
            this McpServer server,
            IFormElicitationSource form,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(server);
            ArgumentNullException.ThrowIfNull(form);
            return server.ElicitAsync(form.Build(), cancellationToken);
        }

        /// <summary>
        /// Builds <paramref name="form"/>, elicits it, and deserializes accepted content as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The form type. Inferred from <see cref="FormElicitation.From{T}"/>.</typeparam>
        /// <param name="server">The MCP server.</param>
        /// <param name="form">A typed form builder or field facade.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The typed elicitation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="server"/> or <paramref name="form"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// var result = await server.ElicitAsync(
        ///     FormElicitation.From&lt;ProductQuery&gt;("Query products").Exclude(x =&gt; x.Skip),
        ///     cancellationToken);
        /// if (result.IsAccepted) { Use(result.Content); }
        /// </code>
        /// </example>
        [RequiresUnreferencedCode("Deserializing form content requires the public properties of T. Use the JsonSerializerOptions overload with a JsonSerializerContext for native AOT.")]
        [RequiresDynamicCode("Deserializing form content requires the public properties of T. Use the JsonSerializerOptions overload with a JsonSerializerContext for native AOT.")]
        public static ValueTask<ElicitResult<T>> ElicitAsync<T>(
            this McpServer server,
            IFormElicitationSource<T> form,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(server);
            ArgumentNullException.ThrowIfNull(form);
            return ElicitAsync<T>(server, form, options: null, cancellationToken);
        }

        /// <summary>
        /// Builds an untyped <paramref name="form"/>, elicits it, and deserializes accepted content as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type to bind accepted content onto.</typeparam>
        /// <param name="server">The MCP server.</param>
        /// <param name="form">A form built field-by-field, typically from metadata.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The typed elicitation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="server"/> or <paramref name="form"/> is <see langword="null"/>.
        /// </exception>
        [RequiresUnreferencedCode("Deserializing form content requires the public properties of T. Use the JsonSerializerOptions overload with a JsonSerializerContext for native AOT.")]
        [RequiresDynamicCode("Deserializing form content requires the public properties of T. Use the JsonSerializerOptions overload with a JsonSerializerContext for native AOT.")]
        public static ValueTask<ElicitResult<T>> ElicitAsync<T>(
            this McpServer server,
            IFormElicitationSource form,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(server);
            ArgumentNullException.ThrowIfNull(form);
            return ElicitAsync<T>(server, form, options: null, cancellationToken);
        }

        /// <summary>
        /// Builds <paramref name="form"/>, elicits it, and deserializes accepted content as <typeparamref name="T"/>
        /// using <paramref name="options"/>.
        /// </summary>
        /// <typeparam name="T">The form type.</typeparam>
        /// <param name="server">The MCP server.</param>
        /// <param name="form">Any form source, including an untyped builder filled from metadata.</param>
        /// <param name="options">
        /// Serializer options. When <see langword="null"/>, properties are camelCase and enums use their member names.
        /// Native AOT callers should pass options from a <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The typed elicitation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="server"/> or <paramref name="form"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Use this overload when the form was built from string field names (EDM properties, query options)
        /// and you still want a CLR DTO back.
        /// </remarks>
        [RequiresUnreferencedCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [RequiresDynamicCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Bind is annotated; AOT callers pass source-generated options.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Bind is annotated; AOT callers pass source-generated options.")]
        public static async ValueTask<ElicitResult<T>> ElicitAsync<T>(
            this McpServer server,
            IFormElicitationSource form,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(server);
            ArgumentNullException.ThrowIfNull(form);

            var raw = await server.ElicitAsync(form.Build(), cancellationToken).ConfigureAwait(false);
            return FormElicitation.Bind<T>(raw, options);
        }

        #endregion

    }

}
