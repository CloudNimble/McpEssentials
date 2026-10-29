namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Two properties that camelCase to the same wire name.
    /// </summary>
    public class CollisionForm
    {

        public string EmailAddress { get; set; } = "";

        public string emailAddress { get; set; } = "";

    }

}
