using System;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// Selects Tasks wrapping from <see cref="McpElicitFormAttribute"/> and
    /// <see cref="McpElicitUrlAttribute"/> when Tasks is registered.
    /// </summary>
    public static class FormElicitationExecutionMode
    {

        #region Public Methods

        /// <summary>
        /// Wraps <see cref="McpTasksOptions.ExecutionModeSelector"/> so a
        /// <see cref="McpElicitFormAttribute"/> tool stays on <c>tools/call</c>
        /// (<see cref="McpTaskExecutionMode.Synchronous"/>) and a
        /// <see cref="McpElicitUrlAttribute"/> tool is
        /// <see cref="McpTaskExecutionMode.Optional"/>.
        /// </summary>
        /// <param name="options">The Tasks options being configured.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Form wins when both attributes are present. URL tools wrap only when the client
        /// advertised Tasks on this call, so a waiter can outlive the POST.
        /// </remarks>
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
                    : IsUrlTool(context.MatchedPrimitive)
                        ? McpTaskExecutionMode.Optional
                        : inner(context);
        }

        /// <summary>
        /// Returns whether <paramref name="context"/> matched a tool stamped with
        /// <see cref="McpElicitFormAttribute"/>.
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
        /// <see cref="McpElicitFormAttribute"/> on the type or method.
        /// </summary>
        /// <param name="primitive">The matched tool, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the attribute is present.</returns>
        public static bool IsFormTool(IMcpServerPrimitive? primitive)
        {
            return HasAttribute<McpElicitFormAttribute>(primitive);
        }

        /// <summary>
        /// Returns whether <paramref name="context"/> matched a tool stamped with
        /// <see cref="McpElicitUrlAttribute"/>.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns><see langword="true"/> when the matched primitive carries the attribute.</returns>
        public static bool IsUrlTool(RequestContext<CallToolRequestParams> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return IsUrlTool(context.MatchedPrimitive);
        }

        /// <summary>
        /// Returns whether <paramref name="primitive"/> carries
        /// <see cref="McpElicitUrlAttribute"/> on the type or method.
        /// </summary>
        /// <param name="primitive">The matched tool, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the attribute is present.</returns>
        public static bool IsUrlTool(IMcpServerPrimitive? primitive)
        {
            return HasAttribute<McpElicitUrlAttribute>(primitive);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Returns whether <paramref name="primitive"/> metadata contains <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The attribute type to find.</typeparam>
        /// <param name="primitive">The matched tool, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when an instance of <typeparamref name="T"/> is present.</returns>
        internal static bool HasAttribute<T>(IMcpServerPrimitive? primitive)
            where T : Attribute
        {
            if (primitive?.Metadata is not { Count: > 0 } metadata)
            {
                return false;
            }

            for (var i = 0; i < metadata.Count; i++)
            {
                if (metadata[i] is T)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

    }

}
