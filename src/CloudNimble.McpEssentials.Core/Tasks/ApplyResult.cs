namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// Result of <see cref="InMemoryMcpTaskStorePlus.TryApply"/>.
    /// </summary>
    public enum ApplyResult
    {
        /// <summary>No snapshot was present, or <see cref="StoredMcpTask.HasExpired"/> was <see langword="true"/>.</summary>
        Missing = 0,

        /// <summary>The snapshot was present and left unchanged.</summary>
        Unchanged = 1,

        /// <summary>The snapshot was replaced.</summary>
        Updated = 2
    }

}
