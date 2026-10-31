namespace CloudNimble.McpEssentials.Cosmos.Tasks
{

    /// <summary>
    /// A task document plus the Cosmos ETag used for compare-and-replace.
    /// </summary>
    /// <param name="Document">The task snapshot.</param>
    /// <param name="ETag">The item ETag.</param>
    internal readonly record struct CosmosTaskRead(CosmosStoredMcpTask Document, string ETag);

}
