using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Enables MCP Tasks backed by Entity Framework Core on an <see cref="IMcpServerBuilder"/>.
    /// </summary>
    public static class McpEssentials_EntityFrameworkCore_McpServerBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="EFCoreMcpTaskStore{TContext}"/> for a single user.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IMcpServerBuilder WithTasksPlus<TContext>(
            this IMcpServerBuilder builder,
            string userId,
            Action<EFCoreMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
            where TContext : DbContext
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return builder.WithTasksPlus(tasks => tasks.UseEntityFramework<TContext>(userId, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="EFCoreMcpTaskStore{TContext}"/>.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithTasksPlus<TContext>(
            this IMcpServerBuilder builder,
            Func<string?> getCurrentUserId,
            Action<EFCoreMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return builder.WithTasksPlus(tasks => tasks.UseEntityFramework<TContext>(getCurrentUserId, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="EFCoreMcpTaskStore{TContext}"/>,
        /// resolving the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithTasksPlus<TContext>(
            this IMcpServerBuilder builder,
            Func<ClaimsPrincipal?> getPrincipal,
            Action<EFCoreMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithTasksPlus(tasks => tasks.UseEntityFramework<TContext>(getPrincipal, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="EFCoreMcpTaskStore{TContext}"/>,
        /// resolving the user from services already registered in the built <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. ASP.NET Core hosts call
        /// <c>AddHttpContextAccessor()</c> and pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        /// <example>
        /// <code>
        /// builder.Services.AddHttpContextAccessor();
        /// builder.Services.AddDbContext&lt;AppDbContext&gt;(o =&gt; o.UseSqlServer(connectionString));
        /// builder.Services.AddMcpServer()
        ///     .WithHttpTransport()
        ///     .WithTasksPlus(tasks =&gt; tasks.UseEntityFramework&lt;AppDbContext&gt;(
        ///         sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User));
        /// </code>
        /// </example>
        public static IMcpServerBuilder WithTasksPlus<TContext>(
            this IMcpServerBuilder builder,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<EFCoreMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithTasksPlus(tasks => tasks.UseEntityFramework<TContext>(getPrincipal, configure), configureTasks);
        }

        #endregion

    }

}
