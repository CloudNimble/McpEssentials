using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Seeds a <see cref="FormElicitationBuilder"/> from the public instance properties of a CLR type.
    /// Unmappable members are skipped. Two properties that camelCase to the same wire name throw.
    /// </summary>
    internal static class FormElicitationConventions
    {

        #region Public Methods

        /// <summary>
        /// Adds a field for every mappable public instance property on <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The form type.</typeparam>
        /// <param name="builder">The builder to seed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when two properties collapse to the same camelCase name.</exception>
        public static void Apply<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(FormElicitationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var context = new NullabilityInfoContext();
            foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length > 0 || !property.CanRead)
                {
                    continue;
                }

                if (!TryCreateField(property, context, out var field))
                {
                    continue;
                }

                var existing = builder.Fields.Find(f => f.Name == field.Name);
                if (existing is not null)
                {
                    throw new InvalidOperationException(
                        $"Properties that map to form field '{field.Name}' collide. Rename one of them or Exclude one.");
                }

                builder.Fields.Add(field);
            }
        }

        /// <summary>
        /// Tries to map <paramref name="property"/> onto an elicitation field.
        /// </summary>
        /// <param name="property">The CLR property.</param>
        /// <param name="context">Nullability reader for NRT required-vs-optional.</param>
        /// <param name="field">The field when mapping succeeds.</param>
        /// <returns><see langword="true"/> when the property is a supported primitive.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "PropertyType is inspected only to detect primitives and IEnumerable<T> of enum.")]
        public static bool TryCreateField(PropertyInfo property, NullabilityInfoContext context, [NotNullWhen(true)] out ElicitationField? field)
        {
            ArgumentNullException.ThrowIfNull(property);
            ArgumentNullException.ThrowIfNull(context);

            field = null;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (!TryMapKind(type, out var kind, out var format, out var enumType))
            {
                return false;
            }

            var nullability = context.Create(property);
            var required = Attribute.IsDefined(property, typeof(RequiredMemberAttribute))
                || nullability.ReadState == NullabilityState.NotNull;

            field = new ElicitationField
            {
                Name = FormElicitationProperty.WireName(property.Name),
                Title = property.Name.Titleize(),
                Kind = kind,
                Format = format,
                Required = required
            };

            if (enumType is not null)
            {
                foreach (var name in Enum.GetNames(enumType))
                {
                    field.Options.Add(new ElicitationOption
                    {
                        Value = name,
                        Title = name.Titleize()
                    });
                }
            }

            return true;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Maps a CLR type (already unwrapped from <see cref="Nullable{T}"/>) to a field kind.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="kind">The field kind.</param>
        /// <param name="format">A string format, when the type implies one.</param>
        /// <param name="enumType">The enum type to expand into options, when applicable.</param>
        /// <returns><see langword="true"/> when the type is mappable.</returns>
        internal static bool TryMapKind(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
            out ElicitationFieldKind kind,
            out string? format,
            out Type? enumType)
        {
            kind = ElicitationFieldKind.String;
            format = null;
            enumType = null;

            if (type == typeof(string) || type == typeof(Guid) || type == typeof(char))
            {
                kind = ElicitationFieldKind.String;
                return true;
            }

            if (type == typeof(Uri))
            {
                kind = ElicitationFieldKind.String;
                format = "uri";
                return true;
            }

            if (type == typeof(DateOnly))
            {
                kind = ElicitationFieldKind.String;
                format = "date";
                return true;
            }

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                kind = ElicitationFieldKind.String;
                format = "date-time";
                return true;
            }

            if (type == typeof(bool))
            {
                kind = ElicitationFieldKind.Boolean;
                return true;
            }

            if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
            {
                kind = ElicitationFieldKind.Integer;
                return true;
            }

            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                kind = ElicitationFieldKind.Number;
                return true;
            }

            if (type.IsEnum)
            {
                enumType = type;
                kind = type.IsDefined(typeof(FlagsAttribute), inherit: false)
                    ? ElicitationFieldKind.MultiChoice
                    : ElicitationFieldKind.Choice;
                return true;
            }

            if (TryGetEnumEnumerable(type, out var itemEnum))
            {
                enumType = itemEnum;
                kind = ElicitationFieldKind.MultiChoice;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the enum type when <paramref name="type"/> is an array or <see cref="IEnumerable{T}"/> of enum.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="enumType">The enum item type.</param>
        /// <returns><see langword="true"/> when <paramref name="type"/> is an enum sequence.</returns>
        internal static bool TryGetEnumEnumerable(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
            [NotNullWhen(true)] out Type? enumType)
        {
            enumType = null;
            if (type == typeof(string))
            {
                return false;
            }

            if (type.IsArray)
            {
                var element = type.GetElementType();
                if (element is { IsEnum: true })
                {
                    enumType = element;
                    return true;
                }

                return false;
            }

            foreach (var candidate in EnumerateEnumerableArguments(type))
            {
                if (candidate.IsEnum)
                {
                    enumType = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Yields the type arguments of <c>IEnumerable&lt;T&gt;</c> implemented by <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The sequence item types.</returns>
        internal static IEnumerable<Type> EnumerateEnumerableArguments(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                yield return type.GetGenericArguments()[0];
            }

            foreach (var implemented in type.GetInterfaces())
            {
                if (implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    yield return implemented.GetGenericArguments()[0];
                }
            }
        }

        #endregion

    }

}
