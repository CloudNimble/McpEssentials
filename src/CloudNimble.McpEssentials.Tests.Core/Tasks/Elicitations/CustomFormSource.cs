using System.Text.Json;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Custom <see cref="IFormElicitationSource"/> that is not a builder or field facade.
    /// </summary>
    public class CustomFormSource : IFormElicitationSource
    {

        #region Public Methods

        /// <inheritdoc />
        public ElicitRequestParams Build() => FormElicitationBuilder.Create("custom").Build();

        /// <inheritdoc />
        public FormDefinition ToDefinition() => FormElicitationBuilder.Create("custom").ToDefinition();

        /// <inheritdoc />
        public InputRequest ToInputRequest() => FormElicitationBuilder.Create("custom").ToInputRequest();

        /// <inheritdoc />
        public JsonElement? ToJson() => FormElicitationBuilder.Create("custom").ToJson();

        /// <inheritdoc />
        public string ToXml(string? xmlNamespace = null) => FormElicitationBuilder.Create("custom").ToXml(xmlNamespace);

        #endregion

    }

}
