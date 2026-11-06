using System;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Marks a tool so MCP elicits a URL after the member produces one.
    /// <c>WithTasksPlus</c> wraps the call as
    /// <see cref="ModelContextProtocol.Extensions.Tasks.McpTaskExecutionMode.Optional"/> so a
    /// task can outlive the POST while the operator is in the browser.
    /// </summary>
    /// <example>
    /// <code>
    /// [McpServerTool(Name = "Authorize")]
    /// [McpElicitUrl("connectUri")]
    /// public static AuthorizationTicket Authorize()
    /// {
    ///     return ticket;
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Constructor arguments name JSON properties on the result that hold URLs
    /// (<c>connectUri</c>, <c>authorizationUrl</c>, and similar). An empty list leaves the
    /// host to apply its own defaults. Class-level usage applies to every
    /// <c>[McpServerTool]</c> method on the type. When <see cref="McpElicitFormAttribute"/> is
    /// also present, the form wins and the call stays on <c>tools/call</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class McpElicitUrlAttribute : Attribute
    {

        #region Properties

        /// <summary>
        /// Gets JSON property names that hold URLs to elicit.
        /// </summary>
        public string[] PropertyNames { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Marks the member as eliciting URLs from the given result properties.
        /// </summary>
        /// <param name="propertyNames">JSON property names that hold elicit URLs.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyNames"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when a property name is empty.</exception>
        public McpElicitUrlAttribute(params string[] propertyNames)
        {
            ArgumentNullException.ThrowIfNull(propertyNames);
            for (var i = 0; i < propertyNames.Length; i++)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(propertyNames[i]);
            }

            PropertyNames = propertyNames;
        }

        #endregion

    }

}
