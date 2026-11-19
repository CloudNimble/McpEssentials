namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Distinguishes the <c>required</c> keyword from NRT nullability.
    /// </summary>
    public class RequiredKeywordForm
    {

        public required string Code { get; set; }

        public required string? Note { get; set; }

        public string? Hint { get; set; }

    }

}
