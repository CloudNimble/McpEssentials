using System;
using System.Collections.Generic;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Fluent facade for the current multi-select field.
    /// </summary>
    public class MultiChoiceFieldBuilder : IFormElicitationSource
    {

        #region Constructors

        /// <summary>
        /// Initializes a facade for <paramref name="field"/> on <paramref name="form"/>.
        /// </summary>
        /// <param name="form">The parent form.</param>
        /// <param name="field">The current field.</param>
        internal MultiChoiceFieldBuilder(FormElicitationBuilderBase form, ElicitationField field)
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
        public virtual MultiChoiceFieldBuilder Title(string title)
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
        public virtual MultiChoiceFieldBuilder Description(string description)
        {
            ArgumentNullException.ThrowIfNull(description);
            Field.Description = description;
            return this;
        }

        /// <summary>
        /// Marks the field required.
        /// </summary>
        /// <returns>This facade.</returns>
        public virtual MultiChoiceFieldBuilder Required()
        {
            Field.Required = true;
            return this;
        }

        /// <summary>
        /// Marks the field optional.
        /// </summary>
        /// <returns>This facade.</returns>
        public virtual MultiChoiceFieldBuilder Optional()
        {
            Field.Required = false;
            return this;
        }

        /// <summary>
        /// Adds an untitled option.
        /// </summary>
        /// <param name="value">The wire value.</param>
        /// <returns>This facade.</returns>
        public virtual MultiChoiceFieldBuilder Option(string value)
        {
            Field.AddOption(value, title: null);
            return this;
        }

        /// <summary>
        /// Adds a titled option.
        /// </summary>
        /// <param name="value">The wire value.</param>
        /// <param name="title">The display title.</param>
        /// <returns>This facade.</returns>
        public virtual MultiChoiceFieldBuilder Option(string value, string title)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            Field.AddOption(value, title);
            return this;
        }

        /// <summary>
        /// Sets the minimum number of selected items.
        /// </summary>
        /// <param name="count">The minimum. Must be zero or greater.</param>
        /// <returns>This facade.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
        public virtual MultiChoiceFieldBuilder MinItems(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            Field.MinItems = count;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of selected items.
        /// </summary>
        /// <param name="count">The maximum. Must be zero or greater.</param>
        /// <returns>This facade.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
        public virtual MultiChoiceFieldBuilder MaxItems(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            Field.MaxItems = count;
            return this;
        }

        /// <summary>
        /// Sets the default selected values.
        /// </summary>
        /// <param name="values">The default wire values.</param>
        /// <returns>This facade.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> is empty.</exception>
        public virtual MultiChoiceFieldBuilder Default(params string[] values)
        {
            ArgumentNullException.ThrowIfNull(values);
            if (values.Length == 0)
            {
                throw new ArgumentException("Default must include at least one value.", nameof(values));
            }

            Field.DefaultStrings = values;
            return this;
        }

        /// <summary>
        /// Sets the default selected values.
        /// </summary>
        /// <param name="values">The default wire values.</param>
        /// <returns>This facade.</returns>
        public virtual MultiChoiceFieldBuilder Default(IEnumerable<string> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            var list = values as IList<string> ?? [.. values];
            if (list.Count == 0)
            {
                throw new ArgumentException("Default must include at least one value.", nameof(values));
            }

            Field.DefaultStrings = list;
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
