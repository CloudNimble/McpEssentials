using System.Text.Json;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Tests.AspNetCore
{

    /// <summary>
    /// Custom form source with no keyed name, used to prove <c>RequireForm</c> fails first.
    /// </summary>
    public class NamelessFormSource : IFormElicitationSource
    {

        #region Public Methods

        /// <inheritdoc />
        public ElicitRequestParams Build() => FormElicitationBuilder.Create("Nameless").Build();

        /// <inheritdoc />
        public FormDefinition ToDefinition() => FormElicitationBuilder.Create("Nameless").ToDefinition();

        /// <inheritdoc />
        public InputRequest ToInputRequest() => FormElicitationBuilder.Create("Nameless").ToInputRequest();

        /// <inheritdoc />
        public JsonElement? ToJson() => FormElicitationBuilder.Create("Nameless").ToJson();

        /// <inheritdoc />
        public string ToXml(string? xmlNamespace = null) => FormElicitationBuilder.Create("Nameless").ToXml(xmlNamespace);

        #endregion

    }

}
