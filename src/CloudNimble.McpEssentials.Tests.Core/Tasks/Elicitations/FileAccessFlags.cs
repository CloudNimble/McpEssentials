using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Flags enum used to seed multi-choice fields from a form type.
    /// </summary>
    [Flags]
    public enum FileAccessFlags
    {
        Read = 1,
        Write = 2
    }

}
