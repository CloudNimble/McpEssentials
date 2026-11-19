using System;
using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Immutable snapshot of a form elicitation. Produced by
    /// <see cref="IFormElicitationSource.ToDefinition"/> and by <see cref="FormXml.Parse"/>.
    /// </summary>
    public sealed class FormDefinition
    {

        #region Constructors

        /// <summary>
        /// Initializes a form snapshot.
        /// </summary>
        /// <param name="message">The prompt presented with the form.</param>
        /// <param name="fields">The fields in insertion order.</param>
        /// <param name="name">
        /// Optional keyed-DI name when this snapshot is a published copy of a named form.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fields"/> is <see langword="null"/>.</exception>
        public FormDefinition(string message, IReadOnlyList<FormFieldDefinition> fields, string? name = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
            ArgumentNullException.ThrowIfNull(fields);
            Message = message;
            Fields = fields;
            Name = string.IsNullOrWhiteSpace(name) ? null : name;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the fields in insertion order.
        /// </summary>
        public IReadOnlyList<FormFieldDefinition> Fields { get; }

        /// <summary>
        /// Gets the prompt presented with the form.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the optional keyed-DI name, or <see langword="null"/> when the island is anonymous.
        /// </summary>
        public string? Name { get; }

        #endregion

    }

}
