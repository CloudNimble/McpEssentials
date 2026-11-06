using System;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Marks a tool as a form-elicitation tool. <c>WithTasksPlus</c> keeps the call on
    /// <c>tools/call</c> Multi Round-Trip Requests (<see cref="ModelContextProtocol.Extensions.Tasks.McpTaskExecutionMode.Synchronous"/>).
    /// </summary>
    /// <remarks>
    /// Pass the same name to <c>AddFormElicitation</c> and <c>RequireForm</c>.
    /// Class-level usage applies to every <c>[McpServerTool]</c> method on the type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class FormElicitationAttribute : Attribute
    {

        #region Properties

        /// <summary>
        /// Gets the keyed form name.
        /// </summary>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the attribute with the keyed form name.
        /// </summary>
        /// <param name="name">The name passed to <c>AddFormElicitation</c> and <c>RequireForm</c>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
        public FormElicitationAttribute(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
        }

        #endregion

    }

}
