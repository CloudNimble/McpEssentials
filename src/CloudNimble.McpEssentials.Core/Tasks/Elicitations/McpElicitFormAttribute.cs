using System;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Marks a tool so MCP elicits a named form before the member runs.
    /// <c>WithTasksPlus</c> keeps the call on <c>tools/call</c> Multi Round-Trip Requests
    /// (<see cref="ModelContextProtocol.Extensions.Tasks.McpTaskExecutionMode.Synchronous"/>).
    /// </summary>
    /// <example>
    /// <code>
    /// [McpServerTool(Name = "ShipOrder")]
    /// [McpElicitForm("ship")]
    /// public static string ShipOrder(RequestContext&lt;CallToolRequestParams&gt; context)
    /// {
    ///     var result = context.RequireForm("ship");
    ///     if (!result.IsAccepted)
    ///     {
    ///         return "Shipping cancelled.";
    ///     }
    ///
    ///     return $"Shipped as {result.Content!["trackingNumber"].GetString()}.";
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Pass the same name to <c>AddFormElicitation</c> and <c>RequireForm</c>.
    /// Class-level usage applies to every <c>[McpServerTool]</c> method on the type.
    /// When both this attribute and <see cref="McpElicitUrlAttribute"/> are present, the form wins.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class McpElicitFormAttribute : Attribute
    {

        #region Properties

        /// <summary>
        /// Gets the keyed form name.
        /// </summary>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Marks the member as eliciting the named form.
        /// </summary>
        /// <param name="name">The name passed to <c>AddFormElicitation</c> and <c>RequireForm</c>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
        public McpElicitFormAttribute(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
        }

        #endregion

    }

}
