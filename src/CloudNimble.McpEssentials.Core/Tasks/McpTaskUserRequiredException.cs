using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// The exception that is thrown when an authenticated task store is used without a current user.
    /// </summary>
    public class McpTaskUserRequiredException : InvalidOperationException
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTaskUserRequiredException"/> class.
        /// </summary>
        public McpTaskUserRequiredException()
            : base("An authenticated MCP task store requires a logged-in user. Authenticate the request before creating or manipulating tasks.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTaskUserRequiredException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public McpTaskUserRequiredException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTaskUserRequiredException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public McpTaskUserRequiredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion

        #region Internal Methods

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Throw() => throw new McpTaskUserRequiredException();

        #endregion

    }

}
