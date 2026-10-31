// Copyright (c) .NET Foundation and Contributors
//
// The MIT License (MIT)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// Adapted from Humanizer v2.14.1 StringHumanizeExtensions
// https://github.com/Humanizr/Humanizer/blob/v2.14.1/src/Humanizer/StringHumanizeExtensions.cs
//
// Inflector (https://github.com/srkirkland/Inflector)
// The MIT License (MIT)
// Copyright (c) 2013 Scott Kirkland

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Turns PascalCase, camelCase, underscored, and dash-separated identifiers into
    /// space-separated words for form field titles.
    /// </summary>
    /// <remarks>
    /// Algorithm is Humanizer v2.14.1 <c>StringHumanizeExtensions</c>, vendored so Core
    /// does not take a Humanizer package reference. <see cref="Titleize"/> is the form-label
    /// default: humanize, then capitalize the first letter of each word.
    /// </remarks>
    internal static partial class StringHumanizeExtensions
    {

        #region Fields

        private const string PascalCaseWordPartsPattern = @"[\p{Lu}]?[\p{Ll}]+|[0-9]+[\p{Ll}]*|[\p{Lu}]+(?=[\p{Lu}][\p{Ll}]|[0-9]|\b)|[\p{Lo}]+";

        private const string FreestandingSpacingCharPattern = @"\s[-_]|[-_]\s";

        #endregion

        #region Public Methods

        /// <summary>
        /// Humanizes <paramref name="input"/> into space-separated words.
        /// </summary>
        /// <param name="input">The identifier to humanize.</param>
        /// <returns>
        /// A sentence-cased phrase. All-uppercase acronyms are returned unchanged.
        /// </returns>
        /// <example>
        /// <c>"PascalCaseInputString"</c> becomes <c>"Pascal case input string"</c>.
        /// </example>
        public static string Humanize(this string input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (input.Length > 0 && IsAllUpper(input))
            {
                return input;
            }

            if (FreestandingSpacingCharRegex().IsMatch(input))
            {
                return FromPascalCase(FromUnderscoreDashSeparatedWords(input));
            }

            if (input.Contains('_', StringComparison.Ordinal) || input.Contains('-', StringComparison.Ordinal))
            {
                return FromUnderscoreDashSeparatedWords(input);
            }

            return FromPascalCase(input);
        }

        /// <summary>
        /// Humanizes <paramref name="input"/> and title-cases each word for a form label.
        /// </summary>
        /// <param name="input">The identifier to titleize.</param>
        /// <returns>
        /// A title-cased phrase. All-uppercase tokens stay uppercase.
        /// </returns>
        /// <example>
        /// <c>"FirstName"</c> becomes <c>"First Name"</c>.
        /// <c>"HTMLParser"</c> becomes <c>"HTML Parser"</c>.
        /// </example>
        public static string Titleize(this string input)
        {
            var humanized = input.Humanize();
            if (humanized.Length == 0)
            {
                return humanized;
            }

            var builder = new StringBuilder(humanized.Length);
            var changed = false;
            var i = 0;
            while (i < humanized.Length)
            {
                if (humanized[i] == ' ')
                {
                    builder.Append(' ');
                    i++;
                    continue;
                }

                var start = i;
                i++;
                while (i < humanized.Length && humanized[i] != ' ')
                {
                    i++;
                }

                var word = humanized.AsSpan(start, i - start);
                if (word.Length == 0 || IsAllUpper(word))
                {
                    builder.Append(word);
                    continue;
                }

                var titled = char.ToUpperInvariant(word[0]);
                if (titled != word[0])
                {
                    changed = true;
                }

                builder.Append(titled);
                builder.Append(word[1..]);
            }

            return changed ? builder.ToString() : humanized;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Replaces underscores and dashes with spaces.
        /// </summary>
        /// <param name="input">A dash- or underscore-separated identifier.</param>
        /// <returns>The identifier with separators replaced by spaces.</returns>
        private static string FromUnderscoreDashSeparatedWords(string input)
        {
            return string.Create(input.Length, input, static (span, source) =>
            {
                source.AsSpan().CopyTo(span);
                for (var i = 0; i < span.Length; i++)
                {
                    if (span[i] is '_' or '-')
                    {
                        span[i] = ' ';
                    }
                }
            });
        }

        /// <summary>
        /// Splits PascalCase / camelCase into words, lowercasing non-acronym tokens
        /// and capitalizing the first character of the result.
        /// </summary>
        /// <param name="input">A PascalCase or camelCase identifier.</param>
        /// <returns>A space-separated phrase.</returns>
        private static string FromPascalCase(string input)
        {
            var builder = new StringBuilder(input.Length + 8);
            var tokenCount = 0;
            var allKeptUpper = true;
            foreach (var match in PascalCaseWordPartsRegex().EnumerateMatches(input))
            {
                if (tokenCount > 0)
                {
                    builder.Append(' ');
                }

                tokenCount++;
                var word = input.AsSpan(match.Index, match.Length);
                var keepUpper = IsAllUpper(word) &&
                    (word.Length > 1 || (match.Index > 0 && input[match.Index - 1] == ' ') || (word.Length == 1 && word[0] == 'I'));
                if (keepUpper)
                {
                    builder.Append(word);
                    continue;
                }

                allKeptUpper = false;
                for (var i = 0; i < word.Length; i++)
                {
                    builder.Append(char.ToLowerInvariant(word[i]));
                }
            }

            if (tokenCount == 0)
            {
                return input.Length == 0 ? input : string.Empty;
            }

            if (allKeptUpper && tokenCount > 1)
            {
                for (var i = 0; i < builder.Length; i++)
                {
                    builder[i] = char.ToLowerInvariant(builder[i]);
                }
            }

            builder[0] = char.ToUpperInvariant(builder[0]);
            return builder.ToString();
        }

        /// <summary>
        /// Gets whether every character in <paramref name="value"/> is uppercase.
        /// </summary>
        /// <param name="value">The characters to test.</param>
        /// <returns><see langword="true"/> when the span is non-empty and all uppercase, or when it is empty (vacuous).</returns>
        private static bool IsAllUpper(ReadOnlySpan<char> value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsUpper(value[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the compiled PascalCase word-boundary regular expression.
        /// </summary>
        /// <returns>The word-parts regex.</returns>
        [GeneratedRegex(PascalCaseWordPartsPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture)]
        private static partial Regex PascalCaseWordPartsRegex();

        /// <summary>
        /// Gets the compiled regex for a dash or underscore next to whitespace.
        /// </summary>
        /// <returns>The freestanding-separator regex.</returns>
        [GeneratedRegex(FreestandingSpacingCharPattern)]
        private static partial Regex FreestandingSpacingCharRegex();

        #endregion

    }

}
