namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Write-only members are skipped.
    /// </summary>
    public class WriteOnlyForm
    {

        public string Hidden
        {
            set { }
        }

        public string Visible { get; set; } = "";

    }

}
