using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Closed AOT codec for the compact <c>Form</c> / <c>Field</c> / <c>Option</c> island.
    /// Uses <see cref="XmlWriter"/> / <see cref="XmlReader"/>. Does not use
    /// <c>XmlSerializer</c>, sgen, or reflection over the form type.
    /// </summary>
    /// <example>
    /// <code>
    /// var xml = FormXml.Serialize(form.ToDefinition());
    /// var definition = FormXml.Parse(xml);
    /// </code>
    /// </example>
    /// <remarks>
    /// Readers match <see cref="XmlReader.LocalName"/> only. Writers emit <c>xmlns</c>
    /// when <c>xmlNamespace</c> is supplied. DTD processing is prohibited. Comments are
    /// ignored. Processing instructions inside <c>Form</c>, <c>Field</c>, or <c>Option</c>
    /// are unexpected nodes.
    /// </remarks>
    public static class FormXml
    {

        #region Fields

        internal const string DefaultAttribute = "Default";
        internal const string DescriptionAttribute = "Description";
        internal const string FalseLiteral = "false";
        internal const string FieldElement = "Field";
        internal const string FormElement = "Form";
        internal const string FormatAttribute = "Format";
        internal const string MaxAttribute = "Max";
        internal const string MaxLengthAttribute = "MaxLength";
        internal const string MessageAttribute = "Message";
        internal const string MinAttribute = "Min";
        internal const string MinLengthAttribute = "MinLength";
        internal const string NameAttribute = "Name";
        internal const string OptionElement = "Option";
        internal const string RequiredAttribute = "Required";
        internal const string SelectedAttribute = "Selected";
        internal const string TitleAttribute = "Title";
        internal const string TrueLiteral = "true";
        internal const string TypeAttribute = "Type";
        internal const string TypeBoolean = "boolean";
        internal const string TypeEnum = "enum";
        internal const string TypeInteger = "integer";
        internal const string TypeMulti = "multi";
        internal const string TypeNumber = "number";
        internal const string TypeString = "string";
        internal const string ValueAttribute = "Value";
        internal const string XmlNamespaceUri = "http://www.w3.org/XML/1998/namespace";
        internal const string XmlnsNamespaceUri = "http://www.w3.org/2000/xmlns/";

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads a compact form island. Namespace prefixes are ignored.
        /// </summary>
        /// <param name="xml">The island text.</param>
        /// <returns>The snapshot.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="xml"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the island is not a valid form.</exception>
        /// <exception cref="XmlException">Thrown when the text is not well-formed XML or contains a DTD.</exception>
        public static FormDefinition Parse(string xml)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(xml);

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = false,
                IgnoreWhitespace = true
            };

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == FormElement)
                {
                    return ReadForm(reader);
                }
            }

            throw new InvalidOperationException("Form XML must have a Form root element.");
        }

        /// <summary>
        /// Writes a compact form island. Omits the XML declaration.
        /// </summary>
        /// <param name="definition">The snapshot to write.</param>
        /// <param name="xmlNamespace">
        /// Optional namespace URI placed on <c>Form</c> as <c>xmlns</c>.
        /// OData passes the MCP vocabulary document URI.
        /// </param>
        /// <returns>The island text.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition"/> is <see langword="null"/>.</exception>
        public static string Serialize(FormDefinition definition, string? xmlNamespace = null)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Async = false
            };

            var builder = new StringBuilder();
            using var writer = XmlWriter.Create(builder, settings);
            WriteForm(writer, definition, xmlNamespace);
            writer.Flush();
            return builder.ToString();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Maps a compact <c>Type</c> spelling onto <see cref="FormFieldType"/>.
        /// </summary>
        /// <param name="type">The attribute value.</param>
        /// <returns>The field type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="type"/> is unknown.</exception>
        internal static FormFieldType ParseFieldType(string type)
        {
            return type switch
            {
                TypeString => FormFieldType.String,
                TypeInteger => FormFieldType.Integer,
                TypeNumber => FormFieldType.Number,
                TypeBoolean => FormFieldType.Boolean,
                TypeEnum => FormFieldType.Enum,
                TypeMulti => FormFieldType.Multi,
                _ => throw new InvalidOperationException($"Unknown form field Type '{type}'.")
            };
        }

        /// <summary>
        /// Maps <see cref="FormFieldType"/> onto the compact <c>Type</c> spelling.
        /// </summary>
        /// <param name="type">The field type.</param>
        /// <returns>The attribute value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="type"/> is unknown.</exception>
        internal static string ToXmlType(FormFieldType type)
        {
            return type switch
            {
                FormFieldType.String => TypeString,
                FormFieldType.Integer => TypeInteger,
                FormFieldType.Number => TypeNumber,
                FormFieldType.Boolean => TypeBoolean,
                FormFieldType.Enum => TypeEnum,
                FormFieldType.Multi => TypeMulti,
                _ => throw new InvalidOperationException($"Unknown form field type '{type}'.")
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads a boolean attribute that accepts <c>true</c>, <c>True</c>, and <c>1</c>.
        /// </summary>
        /// <param name="value">The attribute value.</param>
        /// <param name="attributeName">The attribute local name, for errors.</param>
        /// <returns><see langword="true"/> when the value is a true token.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is not a boolean token.</exception>
        private static bool ParseBoolean(string value, string attributeName)
        {
            if (string.Equals(value, TrueLiteral, StringComparison.OrdinalIgnoreCase) || value == "1")
            {
                return true;
            }

            if (string.Equals(value, FalseLiteral, StringComparison.OrdinalIgnoreCase) || value == "0")
            {
                return false;
            }

            throw new InvalidOperationException($"Form attribute {attributeName} value '{value}' is not a boolean.");
        }

        /// <summary>
        /// Reads a double using invariant XML conversion.
        /// </summary>
        /// <param name="value">The attribute value.</param>
        /// <param name="attributeName">The attribute local name, for errors.</param>
        /// <returns>The number.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is not a number.</exception>
        private static double ParseDouble(string value, string attributeName)
        {
            try
            {
                var number = XmlConvert.ToDouble(value);
                if (double.IsInfinity(number) || double.IsNaN(number))
                {
                    throw new InvalidOperationException($"Form attribute {attributeName} value '{value}' is not a number.");
                }

                return number;
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new InvalidOperationException($"Form attribute {attributeName} value '{value}' is not a number.", ex);
            }
        }

        /// <summary>
        /// Reads an integer using invariant XML conversion.
        /// </summary>
        /// <param name="value">The attribute value.</param>
        /// <param name="attributeName">The attribute local name, for errors.</param>
        /// <returns>The integer.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is not an integer.</exception>
        private static int ParseInt(string value, string attributeName)
        {
            try
            {
                return XmlConvert.ToInt32(value);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new InvalidOperationException($"Form attribute {attributeName} value '{value}' is not an integer.", ex);
            }
        }

        /// <summary>
        /// Reads one <c>Field</c> element, including nested <c>Option</c> children.
        /// </summary>
        /// <param name="reader">Positioned on the <c>Field</c> start tag.</param>
        /// <returns>The field snapshot.</returns>
        private static FormFieldDefinition ReadField(XmlReader reader)
        {
            string? name = null;
            string? typeText = null;
            var required = false;
            string? format = null;
            string? title = null;
            string? description = null;
            int? minLength = null;
            int? maxLength = null;
            double? minimum = null;
            double? maximum = null;
            int? minItems = null;
            int? maxItems = null;
            string? defaultText = null;

            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    if (IsIgnoredNamespace(reader))
                    {
                        continue;
                    }

                    switch (reader.LocalName)
                    {
                        case NameAttribute:
                            name = reader.Value;
                            break;
                        case TypeAttribute:
                            typeText = reader.Value;
                            break;
                        case RequiredAttribute:
                            required = ParseBoolean(reader.Value, RequiredAttribute);
                            break;
                        case FormatAttribute:
                            format = reader.Value;
                            break;
                        case TitleAttribute:
                            title = reader.Value;
                            break;
                        case DescriptionAttribute:
                            description = reader.Value;
                            break;
                        case MinLengthAttribute:
                            minLength = ParseInt(reader.Value, MinLengthAttribute);
                            break;
                        case MaxLengthAttribute:
                            maxLength = ParseInt(reader.Value, MaxLengthAttribute);
                            break;
                        case MinAttribute:
                            minimum = ParseDouble(reader.Value, MinAttribute);
                            break;
                        case MaxAttribute:
                            maximum = ParseDouble(reader.Value, MaxAttribute);
                            break;
                        case DefaultAttribute:
                            defaultText = reader.Value;
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown form Field attribute '{reader.LocalName}'.");
                    }
                }
                while (reader.MoveToNextAttribute());

                reader.MoveToElement();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Form Field Name is required.");
            }

            if (string.IsNullOrWhiteSpace(typeText))
            {
                throw new InvalidOperationException($"Form Field '{name}' is missing Type.");
            }

            var type = ParseFieldType(typeText);
            if (type is FormFieldType.Enum or FormFieldType.Multi && defaultText is not null)
            {
                throw new InvalidOperationException($"Field '{name}' of Type '{typeText}' must use Selected on Option, not Default.");
            }

            if (type is FormFieldType.Multi)
            {
                try
                {
                    minItems = minimum is { } min ? checked((int)min) : null;
                    maxItems = maximum is { } max ? checked((int)max) : null;
                }
                catch (OverflowException ex)
                {
                    throw new InvalidOperationException($"Field '{name}' Min/Max is out of range for an item count.", ex);
                }

                minimum = null;
                maximum = null;
            }

            var options = new List<FormOptionDefinition>();
            ReadToEnd(reader, $"Form Field '{name}'", child =>
            {
                if (child.LocalName != OptionElement)
                {
                    throw new InvalidOperationException($"Form Field '{name}' has unexpected child '{child.LocalName}'.");
                }

                options.Add(ReadOption(child));
            });

            bool? defaultBool = null;
            double? defaultNumber = null;
            string? defaultString = null;
            IReadOnlyList<string>? defaultStrings = null;
            switch (type)
            {
                case FormFieldType.Boolean when defaultText is not null:
                    defaultBool = ParseBoolean(defaultText, DefaultAttribute);
                    break;
                case FormFieldType.Integer or FormFieldType.Number when defaultText is not null:
                    defaultNumber = ParseDouble(defaultText, DefaultAttribute);
                    break;
                case FormFieldType.String when defaultText is not null:
                    defaultString = defaultText;
                    break;
                case FormFieldType.Enum:
                    defaultString = SelectedValue(options);
                    break;
                case FormFieldType.Multi:
                    defaultStrings = SelectedValues(options);
                    break;
                default:
                    break;
            }

            return new FormFieldDefinition(name, type)
            {
                Required = required,
                Format = format,
                Title = title,
                Description = description,
                MinLength = minLength,
                MaxLength = maxLength,
                Minimum = minimum,
                Maximum = maximum,
                MinItems = minItems,
                MaxItems = maxItems,
                DefaultBool = defaultBool,
                DefaultNumber = defaultNumber,
                DefaultString = defaultString,
                DefaultStrings = defaultStrings,
                Options = options
            };
        }

        /// <summary>
        /// Reads the <c>Form</c> element the reader is positioned on.
        /// </summary>
        /// <param name="reader">Positioned on the <c>Form</c> start tag.</param>
        /// <returns>The form snapshot.</returns>
        private static FormDefinition ReadForm(XmlReader reader)
        {
            string? message = null;
            string? name = null;

            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    if (IsIgnoredNamespace(reader))
                    {
                        continue;
                    }

                    switch (reader.LocalName)
                    {
                        case MessageAttribute:
                            message = reader.Value;
                            break;
                        case NameAttribute:
                            name = reader.Value;
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown form Form attribute '{reader.LocalName}'.");
                    }
                }
                while (reader.MoveToNextAttribute());

                reader.MoveToElement();
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new InvalidOperationException("Form elicitation message cannot be empty.");
            }

            var fields = new List<FormFieldDefinition>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            ReadToEnd(reader, "Form", child =>
            {
                if (child.LocalName != FieldElement)
                {
                    throw new InvalidOperationException($"Form has unexpected child '{child.LocalName}'.");
                }

                var field = ReadField(child);
                if (!seen.Add(field.Name))
                {
                    throw new InvalidOperationException($"Form already has a field named '{field.Name}'.");
                }

                fields.Add(field);
            });

            return new FormDefinition(message, fields, name);
        }

        /// <summary>
        /// Reads one <c>Option</c> element.
        /// </summary>
        /// <param name="reader">Positioned on the <c>Option</c> start tag.</param>
        /// <returns>The option snapshot.</returns>
        private static FormOptionDefinition ReadOption(XmlReader reader)
        {
            string? value = null;
            string? title = null;
            var selected = false;

            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    if (IsIgnoredNamespace(reader))
                    {
                        continue;
                    }

                    switch (reader.LocalName)
                    {
                        case ValueAttribute:
                            value = reader.Value;
                            break;
                        case TitleAttribute:
                            title = reader.Value;
                            break;
                        case SelectedAttribute:
                            selected = ParseBoolean(reader.Value, SelectedAttribute);
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown form Option attribute '{reader.LocalName}'.");
                    }
                }
                while (reader.MoveToNextAttribute());

                reader.MoveToElement();
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Form Option Value is required.");
            }

            ReadToEnd(reader, $"Form Option '{value}'", onElement: null);

            return new FormOptionDefinition(value, title, selected);
        }

        /// <summary>
        /// Gets the single selected option value, or <see langword="null"/> when none is selected.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The selected value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when more than one option is selected.</exception>
        private static string? SelectedValue(List<FormOptionDefinition> options)
        {
            string? selected = null;
            foreach (var option in options)
            {
                if (!option.Selected)
                {
                    continue;
                }

                if (selected is not null)
                {
                    throw new InvalidOperationException("An enum field can have at most one Selected option.");
                }

                selected = option.Value;
            }

            return selected;
        }

        /// <summary>
        /// Gets every selected option value, or <see langword="null"/> when none is selected.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The selected values.</returns>
        private static IReadOnlyList<string>? SelectedValues(List<FormOptionDefinition> options)
        {
            List<string>? selected = null;
            foreach (var option in options)
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

        /// <summary>
        /// Writes one field and its options. Children inherit the <c>Form</c> default namespace.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="field">The field.</param>
        private static void WriteField(XmlWriter writer, FormFieldDefinition field)
        {
            writer.WriteStartElement(FieldElement);
            writer.WriteAttributeString(NameAttribute, field.Name);
            writer.WriteAttributeString(TypeAttribute, ToXmlType(field.Type));
            if (field.Required)
            {
                writer.WriteAttributeString(RequiredAttribute, TrueLiteral);
            }

            WriteOptional(writer, FormatAttribute, field.Format);
            WriteOptional(writer, TitleAttribute, field.Title);
            WriteOptional(writer, DescriptionAttribute, field.Description);

            if (field.MinLength is { } minLength)
            {
                writer.WriteAttributeString(MinLengthAttribute, XmlConvert.ToString(minLength));
            }

            if (field.MaxLength is { } maxLength)
            {
                writer.WriteAttributeString(MaxLengthAttribute, XmlConvert.ToString(maxLength));
            }

            if (field.Type is FormFieldType.Multi)
            {
                if (field.MinItems is { } minItems)
                {
                    writer.WriteAttributeString(MinAttribute, XmlConvert.ToString(minItems));
                }

                if (field.MaxItems is { } maxItems)
                {
                    writer.WriteAttributeString(MaxAttribute, XmlConvert.ToString(maxItems));
                }
            }
            else
            {
                if (field.Minimum is { } minimum)
                {
                    writer.WriteAttributeString(MinAttribute, XmlConvert.ToString(minimum));
                }

                if (field.Maximum is { } maximum)
                {
                    writer.WriteAttributeString(MaxAttribute, XmlConvert.ToString(maximum));
                }
            }

            switch (field.Type)
            {
                case FormFieldType.String when field.DefaultString is not null:
                    writer.WriteAttributeString(DefaultAttribute, field.DefaultString);
                    break;
                case FormFieldType.Integer or FormFieldType.Number when field.DefaultNumber is { } number:
                    writer.WriteAttributeString(DefaultAttribute, XmlConvert.ToString(number));
                    break;
                case FormFieldType.Boolean when field.DefaultBool is { } flag:
                    writer.WriteAttributeString(DefaultAttribute, XmlConvert.ToString(flag));
                    break;
            }

            if (field.Options.Count == 0)
            {
                writer.WriteEndElement();
                return;
            }

            foreach (var option in field.Options)
            {
                WriteOption(writer, option, field);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the <c>Form</c> element and its fields.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="definition">The snapshot.</param>
        /// <param name="xmlNamespace">Optional namespace URI on <c>Form</c>.</param>
        private static void WriteForm(XmlWriter writer, FormDefinition definition, string? xmlNamespace)
        {
            WriteStart(writer, FormElement, xmlNamespace);
            writer.WriteAttributeString(MessageAttribute, definition.Message);
            WriteOptional(writer, NameAttribute, definition.Name);
            foreach (var field in definition.Fields)
            {
                WriteField(writer, field);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes one option. <c>Selected="true"</c> is emitted for the field default.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="option">The option.</param>
        /// <param name="field">The parent field, used to decide <c>Selected</c>.</param>
        private static void WriteOption(XmlWriter writer, FormOptionDefinition option, FormFieldDefinition field)
        {
            writer.WriteStartElement(OptionElement);
            writer.WriteAttributeString(ValueAttribute, option.Value);
            WriteOptional(writer, TitleAttribute, option.Title);
            if (IsSelected(option, field))
            {
                writer.WriteAttributeString(SelectedAttribute, TrueLiteral);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes <paramref name="localName"/> in <paramref name="xmlNamespace"/> when the namespace is set.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="localName">The element local name.</param>
        /// <param name="xmlNamespace">Optional namespace URI.</param>
        private static void WriteStart(XmlWriter writer, string localName, string? xmlNamespace)
        {
            if (string.IsNullOrWhiteSpace(xmlNamespace))
            {
                writer.WriteStartElement(localName);
                return;
            }

            writer.WriteStartElement(localName, xmlNamespace);
        }

        /// <summary>
        /// Writes <paramref name="value"/> when it is not blank.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="localName">The attribute local name.</param>
        /// <param name="value">The value.</param>
        private static void WriteOptional(XmlWriter writer, string localName, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                writer.WriteAttributeString(localName, value);
            }
        }

        /// <summary>
        /// Gets whether <paramref name="option"/> is the (or a) default for <paramref name="field"/>.
        /// </summary>
        /// <param name="option">The option.</param>
        /// <param name="field">The parent field.</param>
        /// <returns><see langword="true"/> when the option should carry <c>Selected="true"</c>.</returns>
        private static bool IsSelected(FormOptionDefinition option, FormFieldDefinition field)
        {
            if (option.Selected)
            {
                return true;
            }

            if (field.Type is FormFieldType.Enum &&
                field.DefaultString is { } single &&
                string.Equals(option.Value, single, StringComparison.Ordinal))
            {
                return true;
            }

            if (field.Type is FormFieldType.Multi && field.DefaultStrings is { } many)
            {
                foreach (var value in many)
                {
                    if (string.Equals(option.Value, value, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets whether the current attribute is xmlns or xml: and should be skipped.
        /// </summary>
        /// <param name="reader">The reader, positioned on an attribute.</param>
        /// <returns><see langword="true"/> when the attribute is a namespace declaration.</returns>
        private static bool IsIgnoredNamespace(XmlReader reader)
        {
            return reader.NamespaceURI is XmlnsNamespaceUri or XmlNamespaceUri;
        }

        /// <summary>
        /// Consumes the element the reader is positioned on. Children must be elements handled
        /// by <paramref name="onElement"/>, which leaves the reader after each child.
        /// </summary>
        /// <param name="reader">Positioned on the start tag.</param>
        /// <param name="owner">The element being read, for errors.</param>
        /// <param name="onElement">
        /// Called for each child element. <see langword="null"/> when the owner forbids children.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown on mixed content, a processing instruction, or an unexpected child.
        /// </exception>
        private static void ReadToEnd(XmlReader reader, string owner, Action<XmlReader>? onElement)
        {
            var empty = reader.IsEmptyElement;
            if (!empty)
            {
                var depth = reader.Depth;
                reader.Read();
                while (reader.Depth > depth)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (onElement is null)
                        {
                            throw new InvalidOperationException($"{owner} has unexpected child '{reader.LocalName}'.");
                        }

                        onElement(reader);
                        continue;
                    }

                    ThrowIfMixedContent(reader, owner);
                    throw new InvalidOperationException($"{owner} has unexpected node '{reader.NodeType}'.");
                }
            }

            reader.Read();
        }

        /// <summary>
        /// Throws when the current node is mixed content. The dialect forbids text and CDATA
        /// inside <c>Form</c>, <c>Field</c>, and <c>Option</c>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="owner">The element being read, for the error message.</param>
        /// <exception cref="InvalidOperationException">Thrown when mixed content is present.</exception>
        private static void ThrowIfMixedContent(XmlReader reader, string owner)
        {
            if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.SignificantWhitespace)
            {
                throw new InvalidOperationException($"{owner} has mixed content.");
            }
        }

        #endregion

    }

}
