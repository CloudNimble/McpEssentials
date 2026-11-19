using System;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Fluent facade for the current integer or number field.
    /// </summary>
    public class NumberFieldBuilder : IFormElicitationSource
    {

        #region Constructors

        /// <summary>
        /// Initializes a facade for <paramref name="field"/> on <paramref name="form"/>.
        /// </summary>
        /// <param name="form">The parent form.</param>
        /// <param name="field">The current field.</param>
        internal NumberFieldBuilder(FormElicitationBuilderBase form, ElicitationField field)
        {
            Form = form;
            Field = field;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the parent form.
        /// </summary>
        protected internal FormElicitationBuilderBase Form { get; }

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
        public virtual NumberFieldBuilder Title(string title)
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
        public virtual NumberFieldBuilder Description(string description)
        {
            ArgumentNullException.ThrowIfNull(description);
            Field.Description = description;
            return this;
        }

        /// <summary>
        /// Marks the field required.
        /// </summary>
        /// <returns>This facade.</returns>
        public virtual NumberFieldBuilder Required()
        {
            Field.Required = true;
            return this;
        }

        /// <summary>
        /// Marks the field optional.
        /// </summary>
        /// <returns>This facade.</returns>
        public virtual NumberFieldBuilder Optional()
        {
            Field.Required = false;
            return this;
        }

        /// <summary>
        /// Sets the minimum value.
        /// </summary>
        /// <param name="minimum">The minimum.</param>
        /// <returns>This facade.</returns>
        public virtual NumberFieldBuilder Min(double minimum)
        {
            Field.Minimum = minimum;
            return this;
        }

        /// <summary>
        /// Sets the maximum value.
        /// </summary>
        /// <param name="maximum">The maximum.</param>
        /// <returns>This facade.</returns>
        public virtual NumberFieldBuilder Max(double maximum)
        {
            Field.Maximum = maximum;
            return this;
        }

        /// <summary>
        /// Sets the inclusive minimum and maximum.
        /// </summary>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        /// <returns>This facade.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="minimum"/> is greater than <paramref name="maximum"/>.</exception>
        public virtual NumberFieldBuilder Between(double minimum, double maximum)
        {
            if (minimum > maximum)
            {
                throw new ArgumentException($"Minimum {minimum} is greater than maximum {maximum}.", nameof(maximum));
            }

            Field.Minimum = minimum;
            Field.Maximum = maximum;
            return this;
        }

        /// <summary>
        /// Sets the default value.
        /// </summary>
        /// <param name="value">The default.</param>
        /// <returns>This facade.</returns>
        public virtual NumberFieldBuilder Default(double value)
        {
            Field.DefaultNumber = value;
            return this;
        }

        /// <inheritdoc cref="FormElicitationBuilderBase.String(string)"/>
        public virtual StringFieldBuilder String(string name) => Form.String(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.Email(string)"/>
        public virtual StringFieldBuilder Email(string name) => Form.Email(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.Uri(string)"/>
        public virtual StringFieldBuilder Uri(string name) => Form.Uri(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.Date(string)"/>
        public virtual StringFieldBuilder Date(string name) => Form.Date(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.DateTime(string)"/>
        public virtual StringFieldBuilder DateTime(string name) => Form.DateTime(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.Integer(string)"/>
        public virtual NumberFieldBuilder Integer(string name) => Form.Integer(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.Number(string)"/>
        public virtual NumberFieldBuilder Number(string name) => Form.Number(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.Boolean(string)"/>
        public virtual BooleanFieldBuilder Boolean(string name) => Form.Boolean(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.Choice(string)"/>
        public virtual ChoiceFieldBuilder Choice(string name) => Form.Choice(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.MultiChoice(string)"/>
        public virtual MultiChoiceFieldBuilder MultiChoice(string name) => Form.MultiChoice(name);

        /// <inheritdoc cref="FormElicitationBuilderBase.Build"/>
        public virtual ElicitRequestParams Build() => Form.Build();

        /// <inheritdoc cref="FormElicitationBuilderBase.ToInputRequest"/>
        public virtual InputRequest ToInputRequest() => Form.ToInputRequest();

        /// <inheritdoc cref="FormElicitationBuilderBase.ToDefinition"/>
        public virtual FormDefinition ToDefinition() => Form.ToDefinition();

        /// <inheritdoc cref="FormElicitationBuilderBase.ToXml"/>
        public virtual string ToXml(string? xmlNamespace = null) => Form.ToXml(xmlNamespace);

        /// <inheritdoc cref="FormElicitationBuilderBase.ToJson"/>
        public virtual JsonElement? ToJson() => Form.ToJson();

        #endregion

    }

}
