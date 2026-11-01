using System;
using System.Collections.Generic;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Fluent builder for an MCP form elicitation schema. Field starters return a typed facade
    /// for the current field; the next starter, and <see cref="Build"/>, stay on the chain.
    /// </summary>
    public class FormElicitationBuilder : IFormElicitationSource
    {

        #region Fields

        private ElicitRequestParams? _built;
        private FormDefinition? _definition;
        private ElicitationField? _hotField;
        private InputRequest? _inputRequest;
        private string? _name;
        private bool _sealed;
        private string? _xml;

        #endregion

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
        /// Gets or sets the optional keyed-DI name published on the XML island.
        /// </summary>
        public string? Name
        {
            get => _name;
            set
            {
                ThrowIfSealed();
                _name = value;
            }
        }

        /// <summary>
        /// Gets whether field changes are refused because an output method has already run.
        /// </summary>
        internal bool IsSealed => _sealed;

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
            if (_built is not null)
            {
                return _built;
            }

            if (string.IsNullOrWhiteSpace(Message))
            {
                throw new InvalidOperationException("Form elicitation message cannot be empty.");
            }

            var properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>(Fields.Count, StringComparer.Ordinal);
            List<string>? required = null;
            foreach (var field in Fields)
            {
                properties[field.Name] = field.ToSchema();
                if (field.Required)
                {
                    required ??= new List<string>();
                    required.Add(field.Name);
                }
            }

            _built = new ElicitRequestParams
            {
                Mode = "form",
                Message = Message,
                RequestedSchema = new ElicitRequestParams.RequestSchema
                {
                    Properties = properties,
                    Required = required
                }
            };
            Seal();
            return _built;
        }

        /// <summary>
        /// Builds an <see cref="InputRequest"/> wrapping <see cref="Build"/>.
        /// </summary>
        /// <returns>An elicitation input request for task attachment.</returns>
        public virtual InputRequest ToInputRequest() => _inputRequest ??= InputRequest.ForElicitation(Build());

        /// <summary>
        /// Snapshots this form as an immutable definition.
        /// </summary>
        /// <returns>The field graph independent of the fluent builder.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the message is blank or a field is invalid.</exception>
        public virtual FormDefinition ToDefinition()
        {
            if (_definition is not null)
            {
                return _definition;
            }

            if (string.IsNullOrWhiteSpace(Message))
            {
                throw new InvalidOperationException("Form elicitation message cannot be empty.");
            }

            var fields = new List<FormFieldDefinition>(Fields.Count);
            foreach (var field in Fields)
            {
                field.Validate();
                fields.Add(ToFieldDefinition(field));
            }

            _definition = new FormDefinition(Message, fields, Name);
            Seal();
            return _definition;
        }

        /// <summary>
        /// Serializes this form as a compact XML island.
        /// </summary>
        /// <param name="xmlNamespace">Optional xmlns on the <c>Form</c> element.</param>
        /// <returns>The island text, with no XML declaration.</returns>
        public virtual string ToXml(string? xmlNamespace = null)
            => _xml ??= FormXml.Serialize(ToDefinition(), xmlNamespace);

        /// <summary>
        /// Returns the elicitation JSON already stored on <see cref="InputRequest.Params"/>.
        /// </summary>
        /// <returns>The cached request parameters as JSON.</returns>
        public virtual JsonElement? ToJson() => ToInputRequest().Params;

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
            ThrowIfSealed();
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            var wire = FormElicitationProperty.WireName(name);
            var existing = _hotField is not null && string.Equals(_hotField.Name, wire, StringComparison.Ordinal)
                ? _hotField
                : Fields.Find(f => string.Equals(f.Name, wire, StringComparison.Ordinal));
            if (existing is null)
            {
                var created = new ElicitationField
                {
                    Name = wire,
                    Title = name.Titleize(),
                    Kind = kind
                };
                Fields.Add(created);
                _hotField = created;
                return created;
            }

            if (existing.Kind != kind)
            {
                existing.ResetKind(kind);
            }

            _hotField = existing;
            return existing;
        }

        /// <summary>
        /// Removes the field whose wire name matches <paramref name="name"/>, if present.
        /// </summary>
        /// <param name="name">The field identifier.</param>
        internal void Remove(string name)
        {
            ThrowIfSealed();
            var wire = FormElicitationProperty.WireName(name);
            Fields.RemoveAll(f => string.Equals(f.Name, wire, StringComparison.Ordinal));
            if (_hotField is not null && string.Equals(_hotField.Name, wire, StringComparison.Ordinal))
            {
                _hotField = null;
            }
        }

        /// <summary>
        /// Applies one snapshot field onto this builder using the public starters.
        /// </summary>
        /// <param name="field">The snapshot field.</param>
        /// <exception cref="InvalidOperationException">Thrown when the field type or format is unknown.</exception>
        internal void ApplyField(FormFieldDefinition field)
        {
            ArgumentNullException.ThrowIfNull(field);
            switch (field.Type)
            {
                case FormFieldType.String:
                    ApplyString(field);
                    break;
                case FormFieldType.Integer:
                    ApplyNumber(field, integer: true);
                    break;
                case FormFieldType.Number:
                    ApplyNumber(field, integer: false);
                    break;
                case FormFieldType.Boolean:
                    ApplyBoolean(field);
                    break;
                case FormFieldType.Enum:
                    ApplyChoice(field);
                    break;
                case FormFieldType.Multi:
                    ApplyMultiChoice(field);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown form field type '{field.Type}'.");
            }
        }

        /// <summary>
        /// Applies common title, description, and required facets, overwriting Titleize defaults.
        /// </summary>
        /// <param name="field">The snapshot field.</param>
        /// <param name="target">The live field created by a starter.</param>
        internal static void ApplyCommon(FormFieldDefinition field, ElicitationField target)
        {
            if (!string.IsNullOrWhiteSpace(field.Title))
            {
                target.Title = field.Title;
            }

            if (!string.IsNullOrWhiteSpace(field.Description))
            {
                target.Description = field.Description;
            }

            target.Required = field.Required;
        }

        /// <summary>
        /// Projects a live field onto an immutable snapshot, marking default options as selected.
        /// </summary>
        /// <param name="field">The live field.</param>
        /// <returns>The snapshot field.</returns>
        internal static FormFieldDefinition ToFieldDefinition(ElicitationField field)
        {
            var options = new List<FormOptionDefinition>(field.Options.Count);
            foreach (var option in field.Options)
            {
                var selected = field.Kind switch
                {
                    ElicitationFieldKind.Choice =>
                        field.DefaultString is { } single &&
                        string.Equals(option.Value, single, StringComparison.Ordinal),
                    ElicitationFieldKind.MultiChoice =>
                        field.DefaultStrings is { } many &&
                        ContainsOrdinal(many, option.Value),
                    _ => false
                };
                options.Add(new FormOptionDefinition(option.Value, option.Title, selected));
            }

            return new FormFieldDefinition(field.Name, ToFieldType(field.Kind))
            {
                Required = field.Required,
                Format = field.Format,
                Title = field.Title,
                Description = field.Description,
                MinLength = field.MinLength,
                MaxLength = field.MaxLength,
                Minimum = field.Minimum,
                Maximum = field.Maximum,
                MinItems = field.MinItems,
                MaxItems = field.MaxItems,
                DefaultBool = field.DefaultBool,
                DefaultNumber = field.DefaultNumber,
                DefaultString = field.Kind is ElicitationFieldKind.Choice ? field.DefaultString : field.Kind is ElicitationFieldKind.String ? field.DefaultString : null,
                DefaultStrings = field.DefaultStrings is { } values ? [.. values] : null,
                Options = options
            };
        }

        /// <summary>
        /// Maps a live kind onto the public snapshot type.
        /// </summary>
        /// <param name="kind">The live kind.</param>
        /// <returns>The snapshot type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="kind"/> is unknown.</exception>
        internal static FormFieldType ToFieldType(ElicitationFieldKind kind)
        {
            return kind switch
            {
                ElicitationFieldKind.String => FormFieldType.String,
                ElicitationFieldKind.Integer => FormFieldType.Integer,
                ElicitationFieldKind.Number => FormFieldType.Number,
                ElicitationFieldKind.Boolean => FormFieldType.Boolean,
                ElicitationFieldKind.Choice => FormFieldType.Enum,
                ElicitationFieldKind.MultiChoice => FormFieldType.Multi,
                _ => throw new InvalidOperationException($"Unknown elicitation field kind '{kind}'.")
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Throws when an output method has already materialized this form.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the form is sealed.</exception>
        internal void ThrowIfSealed()
        {
            if (_sealed)
            {
                throw new InvalidOperationException("This form has already been built. Create a new builder to change fields.");
            }
        }

        /// <summary>
        /// Freezes the field list so later starters and facades throw.
        /// </summary>
        private void Seal() => _sealed = true;

        /// <summary>
        /// Applies a boolean snapshot field.
        /// </summary>
        /// <param name="field">The snapshot field.</param>
        private void ApplyBoolean(FormFieldDefinition field)
        {
            var facade = Boolean(field.Name);
            ApplyCommon(field, facade.Field);
            if (field.DefaultBool is { } value)
            {
                facade.Default(value);
            }
        }

        /// <summary>
        /// Applies a single-select snapshot field.
        /// </summary>
        /// <param name="field">The snapshot field.</param>
        private void ApplyChoice(FormFieldDefinition field)
        {
            var facade = Choice(field.Name);
            ApplyCommon(field, facade.Field);
            ApplyOptions(facade.Field, field);
            var selected = field.DefaultString ?? FirstSelected(field);
            if (selected is not null)
            {
                facade.Default(selected);
            }
        }

        /// <summary>
        /// Applies a multi-select snapshot field.
        /// </summary>
        /// <param name="field">The snapshot field.</param>
        private void ApplyMultiChoice(FormFieldDefinition field)
        {
            var facade = MultiChoice(field.Name);
            ApplyCommon(field, facade.Field);
            ApplyOptions(facade.Field, field);
            if (field.MinItems is { } minItems)
            {
                facade.MinItems(minItems);
            }

            if (field.MaxItems is { } maxItems)
            {
                facade.MaxItems(maxItems);
            }

            var selected = field.DefaultStrings ?? SelectedOptionValues(field);
            if (selected is { Count: > 0 })
            {
                facade.Default(selected);
            }
        }

        /// <summary>
        /// Applies an integer or number snapshot field.
        /// </summary>
        /// <param name="field">The snapshot field.</param>
        /// <param name="integer"><see langword="true"/> for integer; otherwise number.</param>
        private void ApplyNumber(FormFieldDefinition field, bool integer)
        {
            var facade = integer ? Integer(field.Name) : Number(field.Name);
            ApplyCommon(field, facade.Field);
            if (field.Minimum is { } minimum && field.Maximum is { } maximum)
            {
                facade.Between(minimum, maximum);
            }
            else
            {
                if (field.Minimum is { } min)
                {
                    facade.Min(min);
                }

                if (field.Maximum is { } max)
                {
                    facade.Max(max);
                }
            }

            if (field.DefaultNumber is { } value)
            {
                facade.Default(value);
            }
        }

        /// <summary>
        /// Applies a string snapshot field, including format starters.
        /// </summary>
        /// <param name="field">The snapshot field.</param>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="FormFieldDefinition.Format"/> is unknown.</exception>
        private void ApplyString(FormFieldDefinition field)
        {
            var facade = field.Format switch
            {
                null or "" => String(field.Name),
                "email" => Email(field.Name),
                "uri" => Uri(field.Name),
                "date" => Date(field.Name),
                "date-time" => DateTime(field.Name),
                _ => throw new InvalidOperationException($"Unknown string format '{field.Format}' on field '{field.Name}'.")
            };
            ApplyCommon(field, facade.Field);
            if (field.MinLength is { } minLength)
            {
                facade.MinLength(minLength);
            }

            if (field.MaxLength is { } maxLength)
            {
                facade.MaxLength(maxLength);
            }

            if (field.DefaultString is not null)
            {
                facade.Default(field.DefaultString);
            }
        }

        /// <summary>
        /// Adds snapshot options onto a live field.
        /// </summary>
        /// <param name="target">The live field.</param>
        /// <param name="field">The snapshot field.</param>
        private static void ApplyOptions(ElicitationField target, FormFieldDefinition field)
        {
            foreach (var option in field.Options)
            {
                target.AddOption(option.Value, option.Title);
            }
        }

        /// <summary>
        /// Gets whether <paramref name="values"/> contains <paramref name="value"/> using ordinal comparison.
        /// </summary>
        /// <param name="values">The list to search.</param>
        /// <param name="value">The value to find.</param>
        /// <returns><see langword="true"/> when the value is present.</returns>
        private static bool ContainsOrdinal(IEnumerable<string> values, string value)
        {
            foreach (var item in values)
            {
                if (string.Equals(item, value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the first selected option value, or <see langword="null"/> when none is selected.
        /// </summary>
        /// <param name="field">The snapshot field.</param>
        /// <returns>The selected value.</returns>
        private static string? FirstSelected(FormFieldDefinition field)
        {
            foreach (var option in field.Options)
            {
                if (option.Selected)
                {
                    return option.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets every selected option value, or <see langword="null"/> when none is selected.
        /// </summary>
        /// <param name="field">The snapshot field.</param>
        /// <returns>The selected values.</returns>
        private static IReadOnlyList<string>? SelectedOptionValues(FormFieldDefinition field)
        {
            List<string>? selected = null;
            foreach (var option in field.Options)
            {
                if (!option.Selected)
                {
                    continue;
                }

                selected ??= [];
                selected.Add(option.Value);
            }

            return selected;
        }

        #endregion

    }

}
