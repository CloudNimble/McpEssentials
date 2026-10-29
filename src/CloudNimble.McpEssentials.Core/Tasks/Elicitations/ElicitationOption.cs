namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// One allowed value on a choice or multi-choice field.
    /// </summary>
    internal class ElicitationOption
    {

        #region Properties

        /// <summary>
        /// Gets or sets the wire value (<c>const</c> / <c>enum</c> entry).
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// Gets or sets the display title, or <see langword="null"/> when the value is untitled.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
