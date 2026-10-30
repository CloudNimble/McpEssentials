using System;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Fluent facade for the current boolean field.
    /// </summary>
    public class BooleanFieldBuilder : IFormElicitationSource
    {

        #region Constructors

        /// <summary>
        /// Initializes a facade for <paramref name="field"/> on <paramref name="form"/>.
        /// </summary>
        /// <param name="form">The parent form.</param>
        /// <param name="field">The current field.</param>
        internal BooleanFieldBuilder(FormElicitationBuilder form, ElicitationField field)
        {
            Form = form;
            Field = field;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the parent form.
        /// </summary>
        protected FormElicitationBuilder Form { get; }

        /// <summary>
        /// Gets the current field.
        /// </summary>
        internal ElicitationField Field { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the display title.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <returns>This facade.</returns>
        public virtual BooleanFieldBuilder Title(string title)
        {
            ArgumentNullException.ThrowIfNull(title);
            Field.Title = title;
            return this;
        }

        /// <summary>
        /// Sets the description.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>This facade.</returns>
        public virtual BooleanFieldBuilder Description(string description)
        {
            ArgumentNullException.ThrowIfNull(description);
            Field.Description = description;
            return this;
        }

        /// <summary>
        /// Marks the field required.
        /// </summary>
        /// <returns>This facade.</returns>
        public virtual BooleanFieldBuilder Required()
        {
            Field.Required = true;
            return this;
        }

        /// <summary>
        /// Marks the field optional.
        /// </summary>
        /// <returns>This facade.</returns>
        public virtual BooleanFieldBuilder Optional()
        {
            Field.Required = false;
            return this;
        }

        /// <summary>
        /// Sets the default value.
        /// </summary>
        /// <param name="value">The default.</param>
        /// <returns>This facade.</returns>
        public virtual BooleanFieldBuilder Default(bool value)
        {
            Field.DefaultBool = value;
            return this;
        }

        /// <inheritdoc cref="FormElicitationBuilder.String(string)"/>
        public virtual StringFieldBuilder String(string name) => Form.String(name);

        /// <inheritdoc cref="FormElicitationBuilder.Email(string)"/>
        public virtual StringFieldBuilder Email(string name) => Form.Email(name);

        /// <inheritdoc cref="FormElicitationBuilder.Uri(string)"/>
        public virtual StringFieldBuilder Uri(string name) => Form.Uri(name);

        /// <inheritdoc cref="FormElicitationBuilder.Date(string)"/>
        public virtual StringFieldBuilder Date(string name) => Form.Date(name);

        /// <inheritdoc cref="FormElicitationBuilder.DateTime(string)"/>
        public virtual StringFieldBuilder DateTime(string name) => Form.DateTime(name);

        /// <inheritdoc cref="FormElicitationBuilder.Integer(string)"/>
        public virtual NumberFieldBuilder Integer(string name) => Form.Integer(name);

        /// <inheritdoc cref="FormElicitationBuilder.Number(string)"/>
        public virtual NumberFieldBuilder Number(string name) => Form.Number(name);

        /// <inheritdoc cref="FormElicitationBuilder.Boolean(string)"/>
        public virtual BooleanFieldBuilder Boolean(string name) => Form.Boolean(name);

        /// <inheritdoc cref="FormElicitationBuilder.Choice(string)"/>
        public virtual ChoiceFieldBuilder Choice(string name) => Form.Choice(name);

        /// <inheritdoc cref="FormElicitationBuilder.MultiChoice(string)"/>
        public virtual MultiChoiceFieldBuilder MultiChoice(string name) => Form.MultiChoice(name);

        /// <inheritdoc cref="FormElicitationBuilder.Build"/>
        public virtual ElicitRequestParams Build() => Form.Build();

        /// <inheritdoc cref="FormElicitationBuilder.ToInputRequest"/>
        public virtual InputRequest ToInputRequest() => Form.ToInputRequest();

        /// <inheritdoc cref="FormElicitationBuilder.ToDefinition"/>
        public virtual FormDefinition ToDefinition() => Form.ToDefinition();

        /// <inheritdoc cref="FormElicitationBuilder.ToXml"/>
        public virtual string ToXml(string? xmlNamespace = null) => Form.ToXml(xmlNamespace);

        #endregion

    }

}
