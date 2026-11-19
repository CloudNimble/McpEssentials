using System;

namespace CloudNimble.McpEssentials.PostgreSQL.Tasks
{

    /// <summary>
    /// Quotes Postgres identifiers after rejecting anything that is not a simple name.
    /// </summary>
    internal static class PostgreSqlIdentifiers
    {

        #region Public Methods

        /// <summary>
        /// Throws when <paramref name="identifier"/> is not a simple Postgres name.
        /// </summary>
        /// <param name="identifier">A schema, table, or <c>LISTEN</c> channel name.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="identifier"/> is empty or contains characters other than
        /// ASCII letters, digits, and underscore.
        /// </exception>
        public static void Validate(string identifier)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            if (identifier.Length > 63 || !IsSafe(identifier))
            {
                throw new ArgumentException(
                    "Postgres identifiers must be 1–63 ASCII letters, digits, or underscores.",
                    nameof(identifier));
            }
        }

        /// <summary>
        /// Returns <paramref name="identifier"/> wrapped in double quotes.
        /// </summary>
        /// <param name="identifier">A schema, table, or <c>LISTEN</c> channel name.</param>
        /// <returns>A quoted identifier safe to interpolate into SQL.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="identifier"/> is empty or contains characters other than
        /// ASCII letters, digits, and underscore.
        /// </exception>
        public static string Quote(string identifier)
        {
            Validate(identifier);
            return "\"" + identifier + "\"";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets whether every character is an ASCII letter, digit, or underscore, starting with a letter or underscore.
        /// </summary>
        /// <param name="identifier">The name to test.</param>
        /// <returns><see langword="true"/> when the name is a simple identifier.</returns>
        private static bool IsSafe(string identifier)
        {
            var first = identifier[0];
            if (!IsLetterOrUnderscore(first))
            {
                return false;
            }

            for (var i = 1; i < identifier.Length; i++)
            {
                var c = identifier[i];
                if (!IsLetterOrUnderscore(c) && (c < '0' || c > '9'))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets whether <paramref name="c"/> is an ASCII letter or underscore.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns><see langword="true"/> for A–Z, a–z, or underscore.</returns>
        private static bool IsLetterOrUnderscore(char c)
            => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';

        #endregion

    }

}
