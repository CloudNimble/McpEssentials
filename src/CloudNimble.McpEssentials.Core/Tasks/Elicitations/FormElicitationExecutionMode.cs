using System;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Keeps form-attributed tools on <c>tools/call</c> Multi Round-Trip Requests when Tasks is registered.
    /// </summary>
    public static class FormElicitationExecutionMode
    {

        #region Public Methods

        /// <summary>
        /// Wraps <see cref="McpTasksOptions.ExecutionModeSelector"/> so a
        /// <see cref="FormElicitationAttribute"/> tool is
        /// <see cref="McpTaskExecutionMode.Synchronous"/>.
        /// </summary>
        /// <param name="options">The Tasks options being configured.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
        public static void Apply(McpTasksOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var inner = options.ExecutionModeSelector;
            if (inner is null)
            {
                options.ExecutionModeSelector = context =>
                    IsFormTool(context.MatchedPrimitive)
                        ? McpTaskExecutionMode.Synchronous
                        : McpTaskExecutionMode.Optional;
                return;
            }

            options.ExecutionModeSelector = context =>
                IsFormTool(context.MatchedPrimitive)
                    ? McpTaskExecutionMode.Synchronous
                    : inner(context);
        }

        /// <summary>
        /// Returns whether <paramref name="context"/> matched a tool stamped with
        /// <see cref="FormElicitationAttribute"/>.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns><see langword="true"/> when the matched primitive carries the attribute.</returns>
        public static bool IsFormTool(RequestContext<CallToolRequestParams> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return IsFormTool(context.MatchedPrimitive);
        }

        /// <summary>
        /// Returns whether <paramref name="primitive"/> carries
        /// <see cref="FormElicitationAttribute"/> on the type or method.
        /// </summary>
        /// <param name="primitive">The matched tool, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the attribute is present.</returns>
        public static bool IsFormTool(IMcpServerPrimitive? primitive)
        {
            if (primitive?.Metadata is not { Count: > 0 } metadata)
            {
                return false;
            }

            for (var i = 0; i < metadata.Count; i++)
            {
                if (metadata[i] is FormElicitationAttribute)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

    }

}
