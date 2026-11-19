using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// A form that can be sent through <c>RequireForm</c> or parked on an MCP task.
    /// The untyped builder, the typed builder, and every field facade implement this.
    /// </summary>
    public interface IFormElicitationSource
    {

        /// <summary>
        /// Builds SDK <see cref="ElicitRequestParams"/> for form mode.
        /// </summary>
        /// <returns>The elicitation request parameters.</returns>
        ElicitRequestParams Build();

        /// <summary>
        /// Builds an <see cref="InputRequest"/> wrapping <see cref="Build"/>.
        /// </summary>
        /// <returns>An elicitation input request for task attachment.</returns>
        InputRequest ToInputRequest();

        /// <summary>
        /// Snapshots this form as an immutable definition.
        /// </summary>
        /// <returns>The field graph independent of the fluent builder.</returns>
        FormDefinition ToDefinition();

        /// <summary>
        /// Serializes this form as a compact XML island.
        /// </summary>
        /// <param name="xmlNamespace">Optional xmlns on the <c>Form</c> element.</param>
        /// <returns>The island text, with no XML declaration.</returns>
        string ToXml(string? xmlNamespace = null);

        /// <summary>
        /// Returns the elicitation JSON already stored on <see cref="InputRequest.Params"/>.
        /// </summary>
        /// <returns>The cached request parameters as JSON.</returns>
        JsonElement? ToJson();

    }

}
