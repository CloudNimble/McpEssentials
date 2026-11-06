using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Redis.Tasks;
using ModelContextProtocol.Extensions.Tasks;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Enables MCP Tasks backed by Redis on an <see cref="IMcpServerBuilder"/>.
    /// </summary>
    public static class McpEssentials_Redis_McpServerBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="RedisMcpTaskStore"/> for a single user.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, key prefix, replica wait).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IMcpServerBuilder WithRedisTasks(
            this IMcpServerBuilder builder,
            string userId,
            Action<RedisMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return builder.WithTasksPlus(tasks => tasks.UseRedis(userId, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="RedisMcpTaskStore"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, key prefix, replica wait).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithRedisTasks(
            this IMcpServerBuilder builder,
            Func<string?> getCurrentUserId,
            Action<RedisMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return builder.WithTasksPlus(tasks => tasks.UseRedis(getCurrentUserId, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="RedisMcpTaskStore"/>,
        /// resolving the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="configure">Optional store options (TTL, key prefix, replica wait).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithRedisTasks(
            this IMcpServerBuilder builder,
            Func<ClaimsPrincipal?> getPrincipal,
            Action<RedisMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithTasksPlus(tasks => tasks.UseRedis(getPrincipal, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="RedisMcpTaskStore"/>,
        /// resolving the user from services already registered in the built <see cref="IServiceProvider"/>.
        /// Register <see cref="IConnectionMultiplexer"/> as a singleton first.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. ASP.NET Core hosts call
        /// <c>AddHttpContextAccessor()</c> and pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, key prefix, replica wait).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        /// <example>
        /// <code>
        /// builder.Services.AddHttpContextAccessor();
        /// builder.Services.AddSingleton&lt;IConnectionMultiplexer&gt;(
        ///     _ =&gt; ConnectionMultiplexer.Connect(connectionString));
        /// builder.Services.AddMcpServer()
        ///     .WithHttpTransport()
        ///     .WithRedisTasks(
        ///         sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User,
        ///         o =&gt; o.DefaultTimeToLive = TimeSpan.FromMinutes(5));
        /// </code>
        /// </example>
        public static IMcpServerBuilder WithRedisTasks(
            this IMcpServerBuilder builder,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<RedisMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithTasksPlus(tasks => tasks.UseRedis(getPrincipal, configure), configureTasks);
        }

        #endregion

    }

}
