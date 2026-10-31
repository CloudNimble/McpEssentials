using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Resolves a simple property-access expression to a <see cref="PropertyInfo"/> and a camelCase wire name.
    /// </summary>
    internal static class FormElicitationProperty
    {

        #region Public Methods

        /// <summary>
        /// Gets the property referenced by <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="T">The form type.</typeparam>
        /// <typeparam name="TProp">The property type.</typeparam>
        /// <param name="expression">A simple access such as <c>x => x.Email</c>.</param>
        /// <returns>The property.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the expression is not a simple property access on <typeparamref name="T"/>.</exception>
        public static PropertyInfo From<T, TProp>(Expression<Func<T, TProp>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            var body = expression.Body;
            if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            {
                body = unary.Operand;
            }

            if (body is MemberExpression { Member: PropertyInfo property, Expression: ParameterExpression })
            {
                return property;
            }

            throw new ArgumentException(
                "Expression must be a simple property access on the form type, such as x => x.Email.",
                nameof(expression));
        }

        /// <summary>
        /// Converts a C# identifier to the camelCase JSON property name used on the wire.
        /// </summary>
        /// <param name="name">The identifier.</param>
        /// <returns>The camelCase name.</returns>
        public static string WireName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (!char.IsUpper(name[0]))
            {
                return name;
            }

            return string.Create(name.Length, name, static (destination, source) =>
            {
                source.AsSpan().CopyTo(destination);
                destination[0] = char.ToLowerInvariant(destination[0]);
            });
        }

        #endregion

    }

}
