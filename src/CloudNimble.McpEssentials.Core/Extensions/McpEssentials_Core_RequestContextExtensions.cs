using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server
{

    /// <summary>
    /// Reads a form answer from <see cref="RequestParams.InputResponses"/> or returns
    /// <see cref="InputRequiredException"/> so the client retries <c>tools/call</c>.
    /// </summary>
    public static class McpEssentials_Core_RequestContextExtensions
    {

        #region Fields

        private static readonly ClientCapabilities RequiredElicitationCapabilities = new()
        {
            Elicitation = new ElicitationCapability
            {
                Form = new FormElicitationCapability()
            }
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Resolves the named form from <see cref="MessageContext.Services"/> and
        /// either returns the client's answer or throws <see cref="InputRequiredException"/>.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <param name="name">The name passed to <c>AddFormElicitation</c>.</param>
        /// <returns>The elicitation result from the retry.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server has no service provider, or no form is registered under <paramref name="name"/>.
        /// </exception>
        /// <exception cref="InputRequiredException">Thrown when the client has not answered yet.</exception>
        /// <example>
        /// <code>
        /// [McpServerTool(Name = "ShipOrder")]
        /// [McpElicitForm("ship")]
        /// public static string ShipOrder(RequestContext&lt;CallToolRequestParams&gt; context)
        /// {
        ///     var result = context.RequireForm("ship");
        ///     if (!result.IsAccepted)
        ///     {
        ///         return "Shipping cancelled.";
        ///     }
        ///
        ///     return $"Shipped as {result.Content!["trackingNumber"].GetString()}.";
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Pair this with <see cref="McpElicitFormAttribute"/> so <c>WithTasksPlus</c> keeps the call synchronous.
        /// </remarks>
        public static ElicitResult RequireForm(this RequestContext<CallToolRequestParams> context, string name)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (TryReadResponse(context, name, out var result))
            {
                return result;
            }

            ThrowIfElicitationMissing(context);
            throw CreateInputRequired(name, ResolveNamedForm(context, name).ToInputRequest());
        }

        /// <summary>
        /// Uses <paramref name="form"/> directly: returns the client's answer or throws
        /// <see cref="InputRequiredException"/>.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <param name="form">A form builder or field facade whose <c>Name</c> is set.</param>
        /// <returns>The elicitation result from the retry.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="context"/> or <paramref name="form"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="form"/> has no name.</exception>
        /// <exception cref="InputRequiredException">Thrown when the client has not answered yet.</exception>
        public static ElicitResult RequireForm(this RequestContext<CallToolRequestParams> context, IFormElicitationSource form)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(form);
            var name = GetFormName(form);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("The form has no name. Register it with AddFormElicitation or set Name before RequireForm.");
            }

            if (TryReadResponse(context, name, out var result))
            {
                return result;
            }

            ThrowIfElicitationMissing(context);
            throw CreateInputRequired(name, form.ToInputRequest());
        }

        /// <summary>
        /// Resolves the named form, then deserializes accepted content as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type to bind accepted content onto.</typeparam>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <param name="name">The name passed to <c>AddFormElicitation</c>.</param>
        /// <param name="options">
        /// Serializer options. When <see langword="null"/>, properties are camelCase and enums use their member names.
        /// Native AOT callers should pass options from a <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>.
        /// </param>
        /// <returns>The typed elicitation result from the retry.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server has no service provider, or no form is registered under <paramref name="name"/>.
        /// </exception>
        /// <exception cref="InputRequiredException">Thrown when the client has not answered yet.</exception>
        [RequiresUnreferencedCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [RequiresDynamicCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "ToTyped is annotated; AOT callers pass source-generated options.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "ToTyped is annotated; AOT callers pass source-generated options.")]
        public static ElicitResult<T> RequireForm<T>(
            this RequestContext<CallToolRequestParams> context,
            string name,
            JsonSerializerOptions? options = null)
        {
            return FormElicitationBinder.ToTyped<T>(context.RequireForm(name), options);
        }

        /// <summary>
        /// Uses <paramref name="form"/> directly, then deserializes accepted content as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type to bind accepted content onto.</typeparam>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <param name="form">A form builder or field facade whose <c>Name</c> is set.</param>
        /// <param name="options">
        /// Serializer options. When <see langword="null"/>, properties are camelCase and enums use their member names.
        /// Native AOT callers should pass options from a <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>.
        /// </param>
        /// <returns>The typed elicitation result from the retry.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="context"/> or <paramref name="form"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="form"/> has no name.</exception>
        /// <exception cref="InputRequiredException">Thrown when the client has not answered yet.</exception>
        [RequiresUnreferencedCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [RequiresDynamicCode("Deserializing form content requires the public properties of T. Pass JsonSerializerOptions from a JsonSerializerContext for native AOT.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "ToTyped is annotated; AOT callers pass source-generated options.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "ToTyped is annotated; AOT callers pass source-generated options.")]
        public static ElicitResult<T> RequireForm<T>(
            this RequestContext<CallToolRequestParams> context,
            IFormElicitationSource form,
            JsonSerializerOptions? options = null)
        {
            return FormElicitationBinder.ToTyped<T>(context.RequireForm(form), options);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Builds the MRTR exception for a single named form request.
        /// </summary>
        /// <param name="name">The input-request key.</param>
        /// <param name="request">The form request.</param>
        /// <returns>An exception the SDK serializes as <c>resultType: input_required</c>.</returns>
        internal static InputRequiredException CreateInputRequired(string name, InputRequest request)
        {
            return new InputRequiredException(new Dictionary<string, InputRequest>(1)
            {
                [name] = request
            });
        }

        /// <summary>
        /// Reads the keyed-DI name from a builder or field facade.
        /// </summary>
        /// <param name="form">A form source.</param>
        /// <returns>The name, or <see langword="null"/> when the source has none.</returns>
        internal static string? GetFormName(IFormElicitationSource form)
        {
            return TryGetBuilder(form)?.Name;
        }

        /// <summary>
        /// Resolves a keyed <see cref="IFormElicitationSource"/> from the request services.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <param name="name">The keyed-DI name.</param>
        /// <returns>The registered form.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server has no service provider, or no form is registered under <paramref name="name"/>.
        /// </exception>
        internal static IFormElicitationSource ResolveNamedForm(RequestContext<CallToolRequestParams> context, string name)
        {
            var services = context.Services ?? context.Server.Services
                ?? throw new InvalidOperationException("The MCP server has no service provider. Register forms with AddFormElicitation.");
            return services.GetRequiredKeyedService<IFormElicitationSource>(name);
        }

        /// <summary>
        /// Throws when the client did not declare form elicitation on this request.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <exception cref="MissingRequiredClientCapabilityException">
        /// Thrown when <c>clientCapabilities.elicitation</c> is missing.
        /// </exception>
        internal static void ThrowIfElicitationMissing(RequestContext<CallToolRequestParams> context)
        {
            var capabilities = context.JsonRpcRequest.Context?.ClientCapabilities ?? context.Server.ClientCapabilities;
            if (capabilities?.Elicitation is not null)
            {
                return;
            }

            throw new MissingRequiredClientCapabilityException(
                RequiredElicitationCapabilities,
                "Server requires the elicitation capability for this request.");
        }

        /// <summary>
        /// Reads an elicitation answer for <paramref name="name"/> from <see cref="RequestParams.InputResponses"/>.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <param name="name">The input-request key.</param>
        /// <param name="result">The deserialized result when the key is present and valid.</param>
        /// <returns><see langword="true"/> when a result was read.</returns>
        internal static bool TryReadResponse(RequestContext<CallToolRequestParams> context, string name, out ElicitResult result)
        {
            result = null!;
            var responses = context.Params?.InputResponses;
            if (responses is null || !responses.TryGetValue(name, out var response) || response is null)
            {
                return false;
            }

            try
            {
                result = response.Deserialize(InputResponse.ElicitResultJsonTypeInfo)!;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the live builder behind <paramref name="form"/>, including field facades.
        /// </summary>
        /// <param name="form">A builder or field facade.</param>
        /// <returns>The parent builder, or <see langword="null"/> for a custom source.</returns>
        internal static FormElicitationBuilderBase? TryGetBuilder(IFormElicitationSource form)
        {
            return form switch
            {
                FormElicitationBuilderBase builder => builder,
                StringFieldBuilder stringField => stringField.Form,
                NumberFieldBuilder numberField => numberField.Form,
                BooleanFieldBuilder booleanField => booleanField.Form,
                ChoiceFieldBuilder choiceField => choiceField.Form,
                MultiChoiceFieldBuilder multiChoiceField => multiChoiceField.Form,
                _ => null
            };
        }

        #endregion

    }

}
