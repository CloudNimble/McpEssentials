using System;
using System.Collections.Generic;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Fluent builder for an MCP form elicitation schema. Field starters return a typed facade
    /// for the current field; the next starter, and <see cref="Build"/>, stay on the chain.
    /// </summary>
    public class FormElicitationBuilder : IFormElicitationSource
    {

        #region Constructors

        /// <summary>
        /// Initializes a builder with <paramref name="message"/> as the prompt.
        /// </summary>
        /// <param name="message">The message presented to the user.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
        public FormElicitationBuilder(string message)
        {
            ArgumentNullException.ThrowIfNull(message);
            Message = message;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the prompt presented with the form.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the fields accumulated so far, in insertion order.
        /// </summary>
        internal List<ElicitationField> Fields { get; } = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds or reconfigures a string field named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder String(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = null;
            return new StringFieldBuilder(this, field);
        }

        /// <summary>
        /// Adds or reconfigures a string field with format <c>email</c>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder Email(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = "email";
            return new StringFieldBuilder(this, field);
        }

        /// <summary>
        /// Adds or reconfigures a string field with format <c>uri</c>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder Uri(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = "uri";
            return new StringFieldBuilder(this, field);
        }

        /// <summary>
        /// Adds or reconfigures a string field with format <c>date</c>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder Date(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = "date";
            return new StringFieldBuilder(this, field);
        }

        /// <summary>
        /// Adds or reconfigures a string field with format <c>date-time</c>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder DateTime(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = "date-time";
            return new StringFieldBuilder(this, field);
        }

        /// <summary>
        /// Adds or reconfigures an integer field named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The number field facade.</returns>
        public virtual NumberFieldBuilder Integer(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.Integer));

        /// <summary>
        /// Adds or reconfigures a floating-point field named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The number field facade.</returns>
        public virtual NumberFieldBuilder Number(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.Number));

        /// <summary>
        /// Adds or reconfigures a boolean field named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The boolean field facade.</returns>
        public virtual BooleanFieldBuilder Boolean(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.Boolean));

        /// <summary>
        /// Adds or reconfigures a single-select field named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The choice field facade.</returns>
        public virtual ChoiceFieldBuilder Choice(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.Choice));

        /// <summary>
        /// Adds or reconfigures a multi-select field named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The field identifier. CamelCased on the wire.</param>
        /// <returns>The multi-choice field facade.</returns>
        public virtual MultiChoiceFieldBuilder MultiChoice(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.MultiChoice));

        /// <summary>
        /// Builds SDK <see cref="ElicitRequestParams"/> for form mode.
        /// </summary>
        /// <returns>The elicitation request parameters.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the message is blank or a field is invalid.</exception>
        public virtual ElicitRequestParams Build()
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                throw new InvalidOperationException("Form elicitation message cannot be empty.");
            }

            var properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>(Fields.Count, StringComparer.Ordinal);
            var required = new List<string>();
            foreach (var field in Fields)
            {
                properties[field.Name] = field.ToSchema();
                if (field.Required)
                {
                    required.Add(field.Name);
                }
            }

            return new ElicitRequestParams
            {
                Mode = "form",
                Message = Message,
                RequestedSchema = new ElicitRequestParams.RequestSchema
                {
                    Properties = properties,
                    Required = required.Count == 0 ? null : required
                }
            };
        }

        /// <summary>
        /// Builds an <see cref="InputRequest"/> wrapping <see cref="Build"/>.
        /// </summary>
        /// <returns>An elicitation input request for task attachment.</returns>
        public virtual InputRequest ToInputRequest() => InputRequest.ForElicitation(Build());

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the field with camelCase name <paramref name="name"/>, adding it when missing.
        /// Reconfiguring a different kind resets kind-specific constraints.
        /// </summary>
        /// <param name="name">The field identifier.</param>
        /// <param name="kind">The field kind.</param>
        /// <returns>The field.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
        internal ElicitationField GetOrAdd(string name, ElicitationFieldKind kind)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            var wire = FormElicitationProperty.WireName(name);
            var existing = Fields.Find(f => string.Equals(f.Name, wire, StringComparison.Ordinal));
            if (existing is null)
            {
                var created = new ElicitationField
                {
                    Name = wire,
                    Title = name.Titleize(),
                    Kind = kind
                };
                Fields.Add(created);
                return created;
            }

            if (existing.Kind != kind)
            {
                existing.ResetKind(kind);
            }

            return existing;
        }

        /// <summary>
        /// Removes the field whose wire name matches <paramref name="name"/>, if present.
        /// </summary>
        /// <param name="name">The field identifier.</param>
        internal void Remove(string name)
        {
            var wire = FormElicitationProperty.WireName(name);
            Fields.RemoveAll(f => string.Equals(f.Name, wire, StringComparison.Ordinal));
        }

        #endregion

    }

}
