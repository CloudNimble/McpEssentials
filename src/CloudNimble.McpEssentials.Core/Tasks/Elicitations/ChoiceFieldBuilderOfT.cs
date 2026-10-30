using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Typed single-select facade. Constraint methods and starters keep <typeparamref name="T"/> on the chain.
    /// </summary>
    /// <typeparam name="T">The form type.</typeparam>
    public class ChoiceFieldBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : ChoiceFieldBuilder, IFormElicitationSource<T>
    {

        /// <summary>
        /// Initializes a facade for <paramref name="field"/> on <paramref name="form"/>.
        /// </summary>
        /// <param name="form">The parent form.</param>
        /// <param name="field">The current field.</param>
        internal ChoiceFieldBuilder(FormElicitationBuilder<T> form, ElicitationField field)
            : base(form, field)
        {
            TypedForm = form;
        }

        /// <summary>
        /// Gets the typed parent form.
        /// </summary>
        protected FormElicitationBuilder<T> TypedForm { get; }

        /// <inheritdoc cref="ChoiceFieldBuilder.Title(string)"/>
        public new ChoiceFieldBuilder<T> Title(string title)
        {
            base.Title(title);
            return this;
        }

        /// <inheritdoc cref="ChoiceFieldBuilder.Description(string)"/>
        public new ChoiceFieldBuilder<T> Description(string description)
        {
            base.Description(description);
            return this;
        }

        /// <inheritdoc cref="ChoiceFieldBuilder.Required"/>
        public new ChoiceFieldBuilder<T> Required()
        {
            base.Required();
            return this;
        }

        /// <inheritdoc cref="ChoiceFieldBuilder.Optional"/>
        public new ChoiceFieldBuilder<T> Optional()
        {
            base.Optional();
            return this;
        }

        /// <inheritdoc cref="ChoiceFieldBuilder.Option(string)"/>
        public new ChoiceFieldBuilder<T> Option(string value)
        {
            base.Option(value);
            return this;
        }

        /// <inheritdoc cref="ChoiceFieldBuilder.Option(string, string)"/>
        public new ChoiceFieldBuilder<T> Option(string value, string title)
        {
            base.Option(value, title);
            return this;
        }

        /// <inheritdoc cref="ChoiceFieldBuilder.Default(string)"/>
        public new ChoiceFieldBuilder<T> Default(string value)
        {
            base.Default(value);
            return this;
        }

        /// <inheritdoc cref="FormElicitationBuilder{T}.Exclude{TProp}"/>
        public virtual FormElicitationBuilder<T> Exclude<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Exclude(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.String(string)"/>
        public new StringFieldBuilder<T> String(string name) => TypedForm.String(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.String{TProp}"/>
        public virtual StringFieldBuilder<T> String<TProp>(Expression<Func<T, TProp>> property) => TypedForm.String(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Email(string)"/>
        public new StringFieldBuilder<T> Email(string name) => TypedForm.Email(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Email{TProp}"/>
        public virtual StringFieldBuilder<T> Email<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Email(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Uri(string)"/>
        public new StringFieldBuilder<T> Uri(string name) => TypedForm.Uri(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Uri{TProp}"/>
        public virtual StringFieldBuilder<T> Uri<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Uri(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Date(string)"/>
        public new StringFieldBuilder<T> Date(string name) => TypedForm.Date(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Date{TProp}"/>
        public virtual StringFieldBuilder<T> Date<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Date(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.DateTime(string)"/>
        public new StringFieldBuilder<T> DateTime(string name) => TypedForm.DateTime(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.DateTime{TProp}"/>
        public virtual StringFieldBuilder<T> DateTime<TProp>(Expression<Func<T, TProp>> property) => TypedForm.DateTime(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Integer(string)"/>
        public new NumberFieldBuilder<T> Integer(string name) => TypedForm.Integer(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Integer{TProp}"/>
        public virtual NumberFieldBuilder<T> Integer<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Integer(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Number(string)"/>
        public new NumberFieldBuilder<T> Number(string name) => TypedForm.Number(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Number{TProp}"/>
        public virtual NumberFieldBuilder<T> Number<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Number(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Boolean(string)"/>
        public new BooleanFieldBuilder<T> Boolean(string name) => TypedForm.Boolean(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Boolean{TProp}"/>
        public virtual BooleanFieldBuilder<T> Boolean<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Boolean(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Choice(string)"/>
        public new ChoiceFieldBuilder<T> Choice(string name) => TypedForm.Choice(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Choice{TProp}"/>
        public virtual ChoiceFieldBuilder<T> Choice<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Choice(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.MultiChoice(string)"/>
        public new MultiChoiceFieldBuilder<T> MultiChoice(string name) => TypedForm.MultiChoice(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.MultiChoice{TProp}"/>
        public virtual MultiChoiceFieldBuilder<T> MultiChoice<TProp>(Expression<Func<T, TProp>> property) => TypedForm.MultiChoice(property);

        /// <inheritdoc cref="FormElicitationBuilder.Build"/>
        public new ElicitRequestParams Build() => TypedForm.Build();

        /// <inheritdoc cref="FormElicitationBuilder.ToInputRequest"/>
        public new InputRequest ToInputRequest() => TypedForm.ToInputRequest();

        /// <inheritdoc cref="FormElicitationBuilder.ToDefinition"/>
        public new FormDefinition ToDefinition() => TypedForm.ToDefinition();

        /// <inheritdoc cref="FormElicitationBuilder.ToXml"/>
        public new string ToXml(string? xmlNamespace = null) => TypedForm.ToXml(xmlNamespace);

    }

}
