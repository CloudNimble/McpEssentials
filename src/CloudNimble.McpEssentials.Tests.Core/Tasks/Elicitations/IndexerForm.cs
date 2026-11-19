namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Indexer must be skipped; <see cref="Name"/> still maps.
    /// </summary>
    public class IndexerForm
    {

        public string Name { get; set; } = "";

        public string this[int index]
        {
            get => Name;
            set => Name = value;
        }

    }

}
