using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Typed form builder. <see cref="FormElicitation.From{T}"/> seeds mappable properties,
    /// then expression starters reconfigure them and string starters add extra fields.
    /// </summary>
    /// <typeparam name="T">The form type.</typeparam>
    public class FormElicitationBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : FormElicitationBuilder, IFormElicitationSource<T>
    {

        #region Constructors

        /// <summary>
        /// Initializes a builder seeded from <typeparamref name="T"/>.
        /// </summary>
        /// <param name="message">The message presented to the user.</param>
        public FormElicitationBuilder(string message)
            : base(message)
        {
            FormElicitationConventions.Apply<T>(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Removes the field for <paramref name="property"/> if it was seeded or added.
        /// No-ops when the property was unmappable or never added.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>This builder.</returns>
        public virtual FormElicitationBuilder<T> Exclude<TProp>(Expression<Func<T, TProp>> property)
        {
            Remove(FormElicitationProperty.From(property).Name);
            return this;
        }

        /// <inheritdoc cref="FormElicitationBuilder.String(string)"/>
        public new StringFieldBuilder<T> String(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = null;
            return new StringFieldBuilder<T>(this, field);
        }

        /// <summary>
        /// Adds or reconfigures a string field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder<T> String<TProp>(Expression<Func<T, TProp>> property) =>
            String(FormElicitationProperty.From(property).Name);

        /// <inheritdoc cref="FormElicitationBuilder.Email(string)"/>
        public new StringFieldBuilder<T> Email(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = "email";
            return new StringFieldBuilder<T>(this, field);
        }

        /// <summary>
        /// Adds or reconfigures an email field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder<T> Email<TProp>(Expression<Func<T, TProp>> property) =>
            Email(FormElicitationProperty.From(property).Name);

        /// <inheritdoc cref="FormElicitationBuilder.Uri(string)"/>
        public new StringFieldBuilder<T> Uri(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = "uri";
            return new StringFieldBuilder<T>(this, field);
        }

        /// <summary>
        /// Adds or reconfigures a URI field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder<T> Uri<TProp>(Expression<Func<T, TProp>> property) =>
            Uri(FormElicitationProperty.From(property).Name);

        /// <inheritdoc cref="FormElicitationBuilder.Date(string)"/>
        public new StringFieldBuilder<T> Date(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = "date";
            return new StringFieldBuilder<T>(this, field);
        }

        /// <summary>
        /// Adds or reconfigures a date field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder<T> Date<TProp>(Expression<Func<T, TProp>> property) =>
            Date(FormElicitationProperty.From(property).Name);

        /// <inheritdoc cref="FormElicitationBuilder.DateTime(string)"/>
        public new StringFieldBuilder<T> DateTime(string name)
        {
            var field = GetOrAdd(name, ElicitationFieldKind.String);
            field.Format = "date-time";
            return new StringFieldBuilder<T>(this, field);
        }

        /// <summary>
        /// Adds or reconfigures a date-time field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The string field facade.</returns>
        public virtual StringFieldBuilder<T> DateTime<TProp>(Expression<Func<T, TProp>> property) =>
            DateTime(FormElicitationProperty.From(property).Name);

        /// <inheritdoc cref="FormElicitationBuilder.Integer(string)"/>
        public new NumberFieldBuilder<T> Integer(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.Integer));

        /// <summary>
        /// Adds or reconfigures an integer field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The number field facade.</returns>
        public virtual NumberFieldBuilder<T> Integer<TProp>(Expression<Func<T, TProp>> property) =>
            Integer(FormElicitationProperty.From(property).Name);

        /// <inheritdoc cref="FormElicitationBuilder.Number(string)"/>
        public new NumberFieldBuilder<T> Number(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.Number));

        /// <summary>
        /// Adds or reconfigures a number field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The number field facade.</returns>
        public virtual NumberFieldBuilder<T> Number<TProp>(Expression<Func<T, TProp>> property) =>
            Number(FormElicitationProperty.From(property).Name);

        /// <inheritdoc cref="FormElicitationBuilder.Boolean(string)"/>
        public new BooleanFieldBuilder<T> Boolean(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.Boolean));

        /// <summary>
        /// Adds or reconfigures a boolean field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The boolean field facade.</returns>
        public virtual BooleanFieldBuilder<T> Boolean<TProp>(Expression<Func<T, TProp>> property) =>
            Boolean(FormElicitationProperty.From(property).Name);

        /// <inheritdoc cref="FormElicitationBuilder.Choice(string)"/>
        public new ChoiceFieldBuilder<T> Choice(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.Choice));

        /// <summary>
        /// Adds or reconfigures a single-select field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The choice field facade.</returns>
        public virtual ChoiceFieldBuilder<T> Choice<TProp>(Expression<Func<T, TProp>> property) =>
            Choice(FormElicitationProperty.From(property).Name);

        /// <inheritdoc cref="FormElicitationBuilder.MultiChoice(string)"/>
        public new MultiChoiceFieldBuilder<T> MultiChoice(string name) =>
            new(this, GetOrAdd(name, ElicitationFieldKind.MultiChoice));

        /// <summary>
        /// Adds or reconfigures a multi-select field for <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="property">A simple property access on <typeparamref name="T"/>.</param>
        /// <returns>The multi-choice field facade.</returns>
        public virtual MultiChoiceFieldBuilder<T> MultiChoice<TProp>(Expression<Func<T, TProp>> property) =>
            MultiChoice(FormElicitationProperty.From(property).Name);

        #endregion

    }

}
