using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Analyzers
{

    /// <summary>
    /// Mirrors Core form conventions against Roslyn symbols so analyzers skip the same shapes runtime skips.
    /// </summary>
    internal static class FormElicitationTypeMapper
    {

        #region Public Methods

        /// <summary>
        /// Gets whether <paramref name="type"/> is a built-in, enum, array, or similar value that is not a form DTO.
        /// </summary>
        /// <param name="type">The type argument of <c>From&lt;T&gt;</c>.</param>
        /// <returns><see langword="true"/> when <c>From</c> should not be used on this type.</returns>
        public static bool IsBuiltInFormType(ITypeSymbol type)
        {
            if (type.TypeKind is TypeKind.Enum or TypeKind.Array or TypeKind.Delegate or TypeKind.Pointer)
            {
                return true;
            }

            if (type.SpecialType is not SpecialType.None and not SpecialType.System_Object)
            {
                return true;
            }

            return IsKnownPrimitive(UnwrapNullable(type));
        }

        /// <summary>
        /// Gets whether <paramref name="type"/> maps onto an elicitation field.
        /// </summary>
        /// <param name="type">A property type, possibly nullable.</param>
        /// <returns><see langword="true"/> when runtime would seed a field.</returns>
        public static bool IsMappable(ITypeSymbol type)
        {
            return TryMap(UnwrapNullable(type));
        }

        /// <summary>
        /// Public instance readable non-indexer properties, derived types hiding base names.
        /// </summary>
        /// <param name="type">The form type.</param>
        /// <returns>Properties in most-derived-first order.</returns>
        public static IReadOnlyList<IPropertySymbol> GetPublicInstanceProperties(ITypeSymbol type)
        {
            var results = new List<IPropertySymbol>();
            var seen = new HashSet<string>();
            for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is not IPropertySymbol property
                        || property.DeclaredAccessibility != Accessibility.Public
                        || property.IsStatic
                        || property.IsIndexer
                        || property.GetMethod is null)
                    {
                        continue;
                    }

                    if (!seen.Add(property.Name))
                    {
                        continue;
                    }

                    results.Add(property);
                }
            }

            return results;
        }

        /// <summary>
        /// Converts a C# identifier to the camelCase JSON property name used on the wire.
        /// </summary>
        /// <param name="name">The identifier.</param>
        /// <returns>The camelCase name.</returns>
        public static string WireName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (char.IsUpper(name[0]))
            {
                return char.ToLowerInvariant(name[0]) + name.Substring(1);
            }

            return name;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Unwraps <c>Nullable&lt;T&gt;</c>.
        /// </summary>
        /// <param name="type">A CLR type symbol.</param>
        /// <returns>The underlying type, or <paramref name="type"/> itself.</returns>
        private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                && named.TypeArguments.Length == 1)
            {
                return named.TypeArguments[0];
            }

            return type;
        }

        /// <summary>
        /// Maps an already-unwrapped type the same way Core conventions do.
        /// </summary>
        /// <param name="type">A non-nullable type symbol.</param>
        /// <returns><see langword="true"/> when the type is a supported primitive or enum sequence.</returns>
        private static bool TryMap(ITypeSymbol type)
        {
            if (IsKnownPrimitive(type) || type.TypeKind == TypeKind.Enum)
            {
                return true;
            }

            if (type.SpecialType == SpecialType.System_String)
            {
                return true;
            }

            if (type is IArrayTypeSymbol array)
            {
                return array.ElementType.TypeKind == TypeKind.Enum;
            }

            return TryGetEnumEnumerable(type);
        }

        /// <summary>
        /// Gets whether <paramref name="type"/> is a primitive Core maps (including Guid, Uri, dates).
        /// </summary>
        /// <param name="type">A type symbol.</param>
        /// <returns><see langword="true"/> for mapped primitives.</returns>
        private static bool IsKnownPrimitive(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                case SpecialType.System_Char:
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_DateTime:
                    return true;
            }

            return IsSystemType(type, "Guid")
                || IsSystemType(type, "Uri")
                || IsSystemType(type, "DateOnly")
                || IsSystemType(type, "DateTimeOffset");
        }

        /// <summary>
        /// Gets whether <paramref name="type"/> is a type in the global <c>System</c> namespace.
        /// </summary>
        /// <param name="type">A type symbol.</param>
        /// <param name="name">The simple type name.</param>
        /// <returns><see langword="true"/> for <c>System.Name</c>.</returns>
        private static bool IsSystemType(ITypeSymbol type, string name)
        {
            return type.Name == name
                && type.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true };
        }

        /// <summary>
        /// Gets whether <paramref name="type"/> is <c>IEnumerable&lt;TEnum&gt;</c>.
        /// String is excluded because it is <c>IEnumerable&lt;char&gt;</c>.
        /// </summary>
        /// <param name="type">A type symbol.</param>
        /// <returns><see langword="true"/> when the sequence item is an enum.</returns>
        private static bool TryGetEnumEnumerable(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_String)
            {
                return false;
            }

            foreach (var argument in EnumerateEnumerableArguments(type))
            {
                if (argument.TypeKind == TypeKind.Enum)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Yields type arguments of implemented <c>IEnumerable&lt;T&gt;</c> interfaces.
        /// </summary>
        /// <param name="type">A type symbol.</param>
        /// <returns>Sequence item types.</returns>
        private static IEnumerable<ITypeSymbol> EnumerateEnumerableArguments(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                && named.TypeArguments.Length == 1)
            {
                yield return named.TypeArguments[0];
            }

            foreach (var implemented in type.AllInterfaces)
            {
                if (implemented.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                    && implemented.TypeArguments.Length == 1)
                {
                    yield return implemented.TypeArguments[0];
                }
            }
        }

        #endregion

    }

}
