using System;
using System.Collections.Generic;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Mutable form field accumulated by the fluent builders. <see cref="ToSchema"/> projects
    /// onto the SDK nested elicitation schema types.
    /// </summary>
    internal class ElicitationField
    {

        #region Properties

        /// <summary>
        /// Gets or sets the camelCase wire name.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the display title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the description shown with the field.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether the field is listed in <c>required</c>.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the primitive kind.
        /// </summary>
        public ElicitationFieldKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the string format: <c>email</c>, <c>uri</c>, <c>date</c>, or <c>date-time</c>.
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets the minimum string length.
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum string length.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the minimum numeric value.
        /// </summary>
        public double? Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum numeric value.
        /// </summary>
        public double? Maximum { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of selected items on a multi-choice field.
        /// </summary>
        public int? MinItems { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of selected items on a multi-choice field.
        /// </summary>
        public int? MaxItems { get; set; }

        /// <summary>
        /// Gets or sets the default string value.
        /// </summary>
        public string? DefaultString { get; set; }

        /// <summary>
        /// Gets or sets the default numeric value.
        /// </summary>
        public double? DefaultNumber { get; set; }

        /// <summary>
        /// Gets or sets the default boolean value.
        /// </summary>
        public bool? DefaultBool { get; set; }

        /// <summary>
        /// Gets or sets the default multi-choice values.
        /// </summary>
        public IList<string>? DefaultStrings { get; set; }

        /// <summary>
        /// Gets the allowed values for choice fields.
        /// </summary>
        public List<ElicitationOption> Options { get; } = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an allowed value. Throws when <paramref name="value"/> is already present.
        /// </summary>
        /// <param name="value">The wire value.</param>
        /// <param name="title">An optional display title.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the value is already an option.</exception>
        public virtual void AddOption(string value, string? title)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (Options.Exists(o => string.Equals(o.Value, value, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"Field '{Name}' already has option '{value}'.");
            }

            Options.Add(new ElicitationOption { Value = value, Title = title });
        }

        /// <summary>
        /// Clears kind-specific constraints and switches to <paramref name="kind"/>.
        /// Keeps name, title, description, and required.
        /// </summary>
        /// <param name="kind">The new kind.</param>
        public virtual void ResetKind(ElicitationFieldKind kind)
        {
            Kind = kind;
            Format = null;
            MinLength = null;
            MaxLength = null;
            Minimum = null;
            Maximum = null;
            MinItems = null;
            MaxItems = null;
            DefaultString = null;
            DefaultNumber = null;
            DefaultBool = null;
            DefaultStrings = null;
            Options.Clear();
        }

        /// <summary>
        /// Throws when the field cannot be projected to a valid MCP schema.
        /// </summary>
        /// <exception cref="InvalidOperationException">The field constraints contradict each other or are incomplete.</exception>
        public virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new InvalidOperationException("Form field name cannot be empty.");
            }

            if (MinLength is { } minLength && MaxLength is { } maxLength && minLength > maxLength)
            {
                throw new InvalidOperationException($"Field '{Name}' has minLength {minLength} greater than maxLength {maxLength}.");
            }

            if (Minimum is { } min && Maximum is { } max && min > max)
            {
                throw new InvalidOperationException($"Field '{Name}' has minimum {min} greater than maximum {max}.");
            }

            if (MinItems is { } minItems && MaxItems is { } maxItems && minItems > maxItems)
            {
                throw new InvalidOperationException($"Field '{Name}' has minItems {minItems} greater than maxItems {maxItems}.");
            }

            if (Kind is ElicitationFieldKind.Choice or ElicitationFieldKind.MultiChoice)
            {
                if (Options.Count == 0)
                {
                    throw new InvalidOperationException($"Choice field '{Name}' has no options.");
                }

                if (DefaultString is { } single && !Options.Exists(o => o.Value == single))
                {
                    throw new InvalidOperationException($"Field '{Name}' default '{single}' is not in the option list.");
                }

                if (DefaultStrings is { } many)
                {
                    foreach (var value in many)
                    {
                        if (!Options.Exists(o => o.Value == value))
                        {
                            throw new InvalidOperationException($"Field '{Name}' default '{value}' is not in the option list.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Projects this field onto an SDK primitive schema.
        /// </summary>
        /// <returns>The SDK schema node.</returns>
        public virtual ElicitRequestParams.PrimitiveSchemaDefinition ToSchema()
        {
            Validate();
            return Kind switch
            {
                ElicitationFieldKind.String => ToStringSchema(),
                ElicitationFieldKind.Integer => ToNumberSchema("integer"),
                ElicitationFieldKind.Number => ToNumberSchema("number"),
                ElicitationFieldKind.Boolean => ToBooleanSchema(),
                ElicitationFieldKind.Choice => ToChoiceSchema(),
                ElicitationFieldKind.MultiChoice => ToMultiChoiceSchema(),
                _ => throw new InvalidOperationException($"Field '{Name}' has unknown kind '{Kind}'.")
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds a string schema.
        /// </summary>
        /// <returns>The SDK string schema.</returns>
        private ElicitRequestParams.StringSchema ToStringSchema()
        {
            return new ElicitRequestParams.StringSchema
            {
                Title = Title,
                Description = Description,
                MinLength = MinLength,
                MaxLength = MaxLength,
                Format = Format,
                Default = DefaultString
            };
        }

        /// <summary>
        /// Builds a number or integer schema.
        /// </summary>
        /// <param name="type">"number" or "integer".</param>
        /// <returns>The SDK number schema.</returns>
        private ElicitRequestParams.NumberSchema ToNumberSchema(string type)
        {
            return new ElicitRequestParams.NumberSchema
            {
                Type = type,
                Title = Title,
                Description = Description,
                Minimum = Minimum,
                Maximum = Maximum,
                Default = DefaultNumber
            };
        }

        /// <summary>
        /// Builds a boolean schema.
        /// </summary>
        /// <returns>The SDK boolean schema.</returns>
        private ElicitRequestParams.BooleanSchema ToBooleanSchema()
        {
            return new ElicitRequestParams.BooleanSchema
            {
                Title = Title,
                Description = Description,
                Default = DefaultBool
            };
        }

        /// <summary>
        /// Builds a single-select enum schema, titled when any option has a title.
        /// </summary>
        /// <returns>The SDK enum schema.</returns>
        private ElicitRequestParams.PrimitiveSchemaDefinition ToChoiceSchema()
        {
            if (HasTitles())
            {
                return new ElicitRequestParams.TitledSingleSelectEnumSchema
                {
                    Title = Title,
                    Description = Description,
                    Default = DefaultString,
                    OneOf = ToEnumOptions()
                };
            }

            return new ElicitRequestParams.UntitledSingleSelectEnumSchema
            {
                Title = Title,
                Description = Description,
                Default = DefaultString,
                Enum = CopyOptionValues()
            };
        }

        /// <summary>
        /// Builds a multi-select enum schema, titled when any option has a title.
        /// </summary>
        /// <returns>The SDK multi-select schema.</returns>
        private ElicitRequestParams.PrimitiveSchemaDefinition ToMultiChoiceSchema()
        {
            if (HasTitles())
            {
                return new ElicitRequestParams.TitledMultiSelectEnumSchema
                {
                    Title = Title,
                    Description = Description,
                    MinItems = MinItems,
                    MaxItems = MaxItems,
                    Default = DefaultStrings,
                    Items = new ElicitRequestParams.TitledEnumItemsSchema
                    {
                        AnyOf = ToEnumOptions()
                    }
                };
            }

            return new ElicitRequestParams.UntitledMultiSelectEnumSchema
            {
                Title = Title,
                Description = Description,
                MinItems = MinItems,
                MaxItems = MaxItems,
                Default = DefaultStrings,
                Items = new ElicitRequestParams.UntitledEnumItemsSchema
                {
                    Enum = CopyOptionValues()
                }
            };
        }

        /// <summary>
        /// Gets whether any option carries a display title.
        /// </summary>
        /// <returns><see langword="true"/> when the schema should be titled.</returns>
        private bool HasTitles()
        {
            return Options.Exists(o => o.Title is not null);
        }

        /// <summary>
        /// Maps options to SDK <see cref="ElicitRequestParams.EnumSchemaOption"/> values.
        /// </summary>
        /// <returns>The titled option list.</returns>
        private List<ElicitRequestParams.EnumSchemaOption> ToEnumOptions()
        {
            var options = new List<ElicitRequestParams.EnumSchemaOption>(Options.Count);
            for (var i = 0; i < Options.Count; i++)
            {
                var option = Options[i];
                options.Add(new ElicitRequestParams.EnumSchemaOption
                {
                    Const = option.Value,
                    Title = option.Title ?? option.Value
                });
            }

            return options;
        }

        /// <summary>
        /// Copies option wire values in insertion order.
        /// </summary>
        /// <returns>The untitled enum list.</returns>
        private List<string> CopyOptionValues()
        {
            var values = new List<string>(Options.Count);
            for (var i = 0; i < Options.Count; i++)
            {
                values.Add(Options[i].Value);
            }

            return values;
        }

        #endregion

    }

}
