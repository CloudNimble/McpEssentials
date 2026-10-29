using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// A form that can be sent through <c>ElicitAsync</c> or parked on an MCP task.
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

    }

}
