using System;
using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Immutable snapshot of one form field. Produced by <see cref="IFormElicitationSource.ToDefinition"/>
    /// and by <see cref="FormXml.Parse"/>.
    /// </summary>
    public sealed class FormFieldDefinition
    {

        #region Constructors

        /// <summary>
        /// Initializes a field with <paramref name="name"/> and <paramref name="type"/>.
        /// Remaining facets are set with object initializers.
        /// </summary>
        /// <param name="name">The camelCase wire name.</param>
        /// <param name="type">The primitive kind.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
        public FormFieldDefinition(string name, FormFieldType type)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
            Type = type;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the default boolean value, or <see langword="null"/> when unset.
        /// </summary>
        public bool? DefaultBool { get; init; }

        /// <summary>
        /// Gets the default numeric value, or <see langword="null"/> when unset.
        /// </summary>
        public double? DefaultNumber { get; init; }

        /// <summary>
        /// Gets the default string or single-select value, or <see langword="null"/> when unset.
        /// </summary>
        public string? DefaultString { get; init; }

        /// <summary>
        /// Gets the default multi-select values, or <see langword="null"/> when unset.
        /// </summary>
        public IReadOnlyList<string>? DefaultStrings { get; init; }

        /// <summary>
        /// Gets the description shown with the field.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets the string format: <c>email</c>, <c>uri</c>, <c>date</c>, or <c>date-time</c>.
        /// </summary>
        public string? Format { get; init; }

        /// <summary>
        /// Gets the maximum number of selected items on a multi-select field.
        /// </summary>
        public int? MaxItems { get; init; }

        /// <summary>
        /// Gets the maximum string length.
        /// </summary>
        public int? MaxLength { get; init; }

        /// <summary>
        /// Gets the maximum numeric value.
        /// </summary>
        public double? Maximum { get; init; }

        /// <summary>
        /// Gets the minimum number of selected items on a multi-select field.
        /// </summary>
        public int? MinItems { get; init; }

        /// <summary>
        /// Gets the minimum string length.
        /// </summary>
        public int? MinLength { get; init; }

        /// <summary>
        /// Gets the minimum numeric value.
        /// </summary>
        public double? Minimum { get; init; }

        /// <summary>
        /// Gets the camelCase wire name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the allowed values for enum and multi fields.
        /// </summary>
        public IReadOnlyList<FormOptionDefinition> Options { get; init; } = [];

        /// <summary>
        /// Gets whether the field is listed in <c>required</c>.
        /// </summary>
        public bool Required { get; init; }

        /// <summary>
        /// Gets the display title.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets the primitive kind.
        /// </summary>
        public FormFieldType Type { get; }

        #endregion

    }

}
