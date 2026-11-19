namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Static properties are ignored; instance properties still map.
    /// </summary>
    public class StaticPropertyForm
    {

        public static string Shared { get; set; } = "";

        public string Name { get; set; } = "";

    }

}
