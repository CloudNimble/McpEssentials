using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using ModelContextProtocol.Extensions.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Enables MCP Tasks on an <see cref="IMcpServerBuilder"/> with a Cosmos store.
    /// </summary>
    public static class McpEssentials_Cosmos_McpServerBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="CosmosMcpTaskStore"/> for a single user.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <param name="configureTasks">Optional SDK Tasks options.</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IMcpServerBuilder WithCosmosTasks(
            this IMcpServerBuilder builder,
            string userId,
            Action<CosmosMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return builder.WithTasksPlus(tasks => tasks.UseCosmos(userId, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="CosmosMcpTaskStore"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <param name="configureTasks">Optional SDK Tasks options.</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithCosmosTasks(
            this IMcpServerBuilder builder,
            Func<string?> getCurrentUserId,
            Action<CosmosMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return builder.WithTasksPlus(tasks => tasks.UseCosmos(getCurrentUserId, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="CosmosMcpTaskStore"/>, resolving the user from a principal.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <param name="configureTasks">Optional SDK Tasks options.</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithCosmosTasks(
            this IMcpServerBuilder builder,
            Func<ClaimsPrincipal?> getPrincipal,
            Action<CosmosMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithTasksPlus(tasks => tasks.UseCosmos(getPrincipal, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="CosmosMcpTaskStore"/>,
        /// resolving the user from services already registered in the built <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. ASP.NET Core hosts call
        /// <c>AddHttpContextAccessor()</c> and pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <param name="configureTasks">Optional SDK Tasks options.</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithCosmosTasks(
            this IMcpServerBuilder builder,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<CosmosMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithTasksPlus(tasks => tasks.UseCosmos(getPrincipal, configure), configureTasks);
        }

        #endregion

    }

}
