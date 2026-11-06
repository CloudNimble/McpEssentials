using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Typed number-field facade. Constraint methods and starters keep <typeparamref name="T"/> on the chain.
    /// </summary>
    /// <typeparam name="T">The form type.</typeparam>
    public class NumberFieldBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : NumberFieldBuilder, IFormElicitationSource<T>
    {

        /// <summary>
        /// Initializes a facade for <paramref name="field"/> on <paramref name="form"/>.
        /// </summary>
        /// <param name="form">The parent form.</param>
        /// <param name="field">The current field.</param>
        internal NumberFieldBuilder(FormElicitationBuilder<T> form, ElicitationField field)
            : base(form, field)
        {
            TypedForm = form;
        }

        /// <summary>
        /// Gets the typed parent form.
        /// </summary>
        protected FormElicitationBuilder<T> TypedForm { get; }

        /// <inheritdoc cref="NumberFieldBuilder.Title(string)"/>
        public override NumberFieldBuilder<T> Title(string title)
        {
            base.Title(title);
            return this;
        }

        /// <inheritdoc cref="NumberFieldBuilder.Description(string)"/>
        public override NumberFieldBuilder<T> Description(string description)
        {
            base.Description(description);
            return this;
        }

        /// <inheritdoc cref="NumberFieldBuilder.Required"/>
        public override NumberFieldBuilder<T> Required()
        {
            base.Required();
            return this;
        }

        /// <inheritdoc cref="NumberFieldBuilder.Optional"/>
        public override NumberFieldBuilder<T> Optional()
        {
            base.Optional();
            return this;
        }

        /// <inheritdoc cref="NumberFieldBuilder.Min(double)"/>
        public override NumberFieldBuilder<T> Min(double minimum)
        {
            base.Min(minimum);
            return this;
        }

        /// <inheritdoc cref="NumberFieldBuilder.Max(double)"/>
        public override NumberFieldBuilder<T> Max(double maximum)
        {
            base.Max(maximum);
            return this;
        }

        /// <inheritdoc cref="NumberFieldBuilder.Between(double, double)"/>
        public override NumberFieldBuilder<T> Between(double minimum, double maximum)
        {
            base.Between(minimum, maximum);
            return this;
        }

        /// <inheritdoc cref="NumberFieldBuilder.Default(double)"/>
        public override NumberFieldBuilder<T> Default(double value)
        {
            base.Default(value);
            return this;
        }

        /// <inheritdoc cref="FormElicitationBuilder{T}.Exclude{TProp}"/>
        public virtual FormElicitationBuilder<T> Exclude<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Exclude(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.String(string)"/>
        public override StringFieldBuilder<T> String(string name) => TypedForm.String(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.String{TProp}"/>
        public virtual StringFieldBuilder<T> String<TProp>(Expression<Func<T, TProp>> property) => TypedForm.String(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Email(string)"/>
        public override StringFieldBuilder<T> Email(string name) => TypedForm.Email(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Email{TProp}"/>
        public virtual StringFieldBuilder<T> Email<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Email(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Uri(string)"/>
        public override StringFieldBuilder<T> Uri(string name) => TypedForm.Uri(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Uri{TProp}"/>
        public virtual StringFieldBuilder<T> Uri<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Uri(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Date(string)"/>
        public override StringFieldBuilder<T> Date(string name) => TypedForm.Date(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Date{TProp}"/>
        public virtual StringFieldBuilder<T> Date<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Date(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.DateTime(string)"/>
        public override StringFieldBuilder<T> DateTime(string name) => TypedForm.DateTime(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.DateTime{TProp}"/>
        public virtual StringFieldBuilder<T> DateTime<TProp>(Expression<Func<T, TProp>> property) => TypedForm.DateTime(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Integer(string)"/>
        public override NumberFieldBuilder<T> Integer(string name) => TypedForm.Integer(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Integer{TProp}"/>
        public virtual NumberFieldBuilder<T> Integer<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Integer(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Number(string)"/>
        public override NumberFieldBuilder<T> Number(string name) => TypedForm.Number(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Number{TProp}"/>
        public virtual NumberFieldBuilder<T> Number<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Number(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Boolean(string)"/>
        public override BooleanFieldBuilder<T> Boolean(string name) => TypedForm.Boolean(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Boolean{TProp}"/>
        public virtual BooleanFieldBuilder<T> Boolean<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Boolean(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Choice(string)"/>
        public override ChoiceFieldBuilder<T> Choice(string name) => TypedForm.Choice(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.Choice{TProp}"/>
        public virtual ChoiceFieldBuilder<T> Choice<TProp>(Expression<Func<T, TProp>> property) => TypedForm.Choice(property);

        /// <inheritdoc cref="FormElicitationBuilder{T}.MultiChoice(string)"/>
        public override MultiChoiceFieldBuilder<T> MultiChoice(string name) => TypedForm.MultiChoice(name);

        /// <inheritdoc cref="FormElicitationBuilder{T}.MultiChoice{TProp}"/>
        public virtual MultiChoiceFieldBuilder<T> MultiChoice<TProp>(Expression<Func<T, TProp>> property) => TypedForm.MultiChoice(property);

        /// <inheritdoc cref="FormElicitationBuilderBase.Build"/>
        public override ElicitRequestParams Build() => TypedForm.Build();

        /// <inheritdoc cref="FormElicitationBuilderBase.ToInputRequest"/>
        public override InputRequest ToInputRequest() => TypedForm.ToInputRequest();

        /// <inheritdoc cref="FormElicitationBuilderBase.ToDefinition"/>
        public override FormDefinition ToDefinition() => TypedForm.ToDefinition();

        /// <inheritdoc cref="FormElicitationBuilderBase.ToXml"/>
        public override string ToXml(string? xmlNamespace = null) => TypedForm.ToXml(xmlNamespace);

        /// <inheritdoc cref="FormElicitationBuilderBase.ToJson"/>
        public override JsonElement? ToJson() => TypedForm.ToJson();

    }

}
