namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Primitive kind of a published form field. Maps onto the compact XML <c>Type</c> attribute
    /// and the MCP elicitation JSON Schema subset.
    /// </summary>
    public enum FormFieldType
    {

        /// <summary>A string, optionally with a format. XML <c>Type="string"</c>.</summary>
        String = 0,

        /// <summary>An integer number. XML <c>Type="integer"</c>.</summary>
        Integer = 1,

        /// <summary>A floating-point number. XML <c>Type="number"</c>.</summary>
        Number = 2,

        /// <summary>A boolean. XML <c>Type="boolean"</c>.</summary>
        Boolean = 3,

        /// <summary>A single-select enum. XML <c>Type="enum"</c>.</summary>
        Enum = 4,

        /// <summary>A multi-select enum. XML <c>Type="multi"</c>.</summary>
        Multi = 5

    }

}
