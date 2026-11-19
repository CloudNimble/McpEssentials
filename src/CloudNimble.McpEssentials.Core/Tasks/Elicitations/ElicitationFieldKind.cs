namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Primitive kind of a form field. Maps onto the MCP elicitation JSON Schema subset.
    /// </summary>
    internal enum ElicitationFieldKind
    {
        /// <summary>A string, optionally with a format.</summary>
        String = 0,

        /// <summary>An integer number.</summary>
        Integer = 1,

        /// <summary>A floating-point number.</summary>
        Number = 2,

        /// <summary>A boolean.</summary>
        Boolean = 3,

        /// <summary>A single-select enum.</summary>
        Choice = 4,

        /// <summary>A multi-select enum.</summary>
        MultiChoice = 5
    }

}
