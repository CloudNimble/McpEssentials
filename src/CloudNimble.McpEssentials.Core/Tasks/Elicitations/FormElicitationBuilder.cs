using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Untyped fluent builder for an MCP form elicitation schema. Start with <see cref="Create"/>
    /// or rebuild from XML with <see cref="ParseXml"/>.
    /// </summary>
    public class FormElicitationBuilder : FormElicitationBuilderBase
    {

        #region Constructors

        /// <summary>
        /// Initializes a builder with <paramref name="message"/> as the prompt.
        /// </summary>
        /// <param name="message">The message presented to the user.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
        protected FormElicitationBuilder(string message)
            : base(message)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts an empty form with <paramref name="message"/> as the prompt.
        /// </summary>
        /// <param name="message">The message presented to the user.</param>
        /// <returns>An untyped form builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
        public static FormElicitationBuilder Create(string message) => new(message);

        /// <summary>
        /// Starts a form seeded from the public instance properties of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The form type. Unmappable properties are skipped.</typeparam>
        /// <param name="message">The message presented to the user.</param>
        /// <returns>A typed form builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
        public static FormElicitationBuilder<T> From<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string message) =>
            FormElicitationBuilder<T>.From(message);

        /// <summary>
        /// Rebuilds an untyped builder from a snapshot.
        /// </summary>
        /// <param name="definition">The snapshot.</param>
        /// <returns>A builder whose <see cref="FormElicitationBuilderBase.Build"/> matches the snapshot.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a field name is duplicated or a facet is invalid.</exception>
        public static FormElicitationBuilder FromDefinition(FormDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            var builder = new FormElicitationBuilder(definition.Message)
            {
                Name = definition.Name
            };
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in definition.Fields)
            {
                ArgumentNullException.ThrowIfNull(field);
                var wire = FormElicitationProperty.WireName(field.Name);
                if (!seen.Add(wire))
                {
                    throw new InvalidOperationException($"Form already has a field named '{field.Name}'.");
                }

                builder.ApplyField(field);
            }

            return builder;
        }

        /// <summary>
        /// Rebuilds an untyped builder from a compact XML island.
        /// </summary>
        /// <param name="xml">The island text.</param>
        /// <returns>A builder whose <see cref="FormElicitationBuilderBase.Build"/> matches the island.</returns>
        public static FormElicitationBuilder ParseXml(string xml) => FromDefinition(FormXml.Parse(xml));

        #endregion

    }

}
