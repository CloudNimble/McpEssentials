using System;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// One allowed value on an <see cref="FormFieldType.Enum"/> or <see cref="FormFieldType.Multi"/> field.
    /// </summary>
    public sealed class FormOptionDefinition
    {

        #region Constructors

        /// <summary>
        /// Initializes an option.
        /// </summary>
        /// <param name="value">The wire value.</param>
        /// <param name="title">An optional display title.</param>
        /// <param name="selected">
        /// <see langword="true"/> when this value is the (or a) default.
        /// Written as <c>Selected="true"</c> on the compact island.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
        public FormOptionDefinition(string value, string? title = null, bool selected = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            Value = value;
            Title = title;
            Selected = selected;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether this option is selected by default.
        /// </summary>
        public bool Selected { get; }

        /// <summary>
        /// Gets the display title, or <see langword="null"/> when the value is untitled.
        /// </summary>
        public string? Title { get; }

        /// <summary>
        /// Gets the wire value.
        /// </summary>
        public string Value { get; }

        #endregion

    }

}
