using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Enables authenticated MCP Tasks backed by Entity Framework Core on an <see cref="IMcpServerBuilder"/>.
    /// </summary>
    public static class McpEssentials_EntityFrameworkCore_McpServerBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/> for a single user.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IMcpServerBuilder WithEntityFrameworkTasks<TContext>(
            this IMcpServerBuilder builder,
            string userId,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
            where TContext : DbContext
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return builder.WithEntityFrameworkTasks<TContext>(_ => userId, configure, configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/>.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithEntityFrameworkTasks<TContext>(
            this IMcpServerBuilder builder,
            Func<string?> getCurrentUserId,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return builder.WithEntityFrameworkTasks<TContext>(_ => getCurrentUserId(), configure, configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/>,
        /// resolving the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithEntityFrameworkTasks<TContext>(
            this IMcpServerBuilder builder,
            Func<ClaimsPrincipal?> getPrincipal,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithEntityFrameworkTasks<TContext>(
                _ => AuthenticatedMcpTaskStoreBase.GetUserId(getPrincipal()),
                configure,
                configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/>,
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
        ///     .WithEntityFrameworkTasks&lt;AppDbContext&gt;(
        ///         sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User,
        ///         o =&gt; o.DefaultTimeToLive = TimeSpan.FromMinutes(5));
        /// </code>
        /// </example>
        public static IMcpServerBuilder WithEntityFrameworkTasks<TContext>(
            this IMcpServerBuilder builder,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithEntityFrameworkTasks<TContext>(
                sp => AuthenticatedMcpTaskStoreBase.GetUserId(getPrincipal(sp)),
                configure,
                configureTasks);
        }

        #endregion

        #region Private Methods

        private static void NoopMcpServerOptions(McpServerOptions _)
        {
        }

        private static IMcpServerBuilder WithEntityFrameworkTasks<TContext>(
            this IMcpServerBuilder builder,
            Func<IServiceProvider, string?> getCurrentUserId,
            Action<AuthenticatedMcpTaskStoreOptions>? configure,
            Action<McpTasksOptions>? configureTasks)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(getCurrentUserId);

            var services = new StrongBox<IServiceProvider>();
            var store = new EntityFrameworkAuthenticatedTaskStore<TContext>(
                () => services.Value!.GetRequiredService<IServiceScopeFactory>(),
                () => getCurrentUserId(services.Value!),
                () => services.Value!.GetService<IMcpTaskInputSignalBus>());
            AuthenticatedMcpTaskStoreOptions.Apply(configure, store);

            builder.Services.TryAddSingleton(sp =>
            {
                services.Value = sp;
                return store;
            });
            builder.Services.TryAddSingleton<IPerUserMcpTaskStore>(sp => sp.GetRequiredService<EntityFrameworkAuthenticatedTaskStore<TContext>>());
            builder.Services.TryAddSingleton<IMcpTaskStore>(sp => sp.GetRequiredService<EntityFrameworkAuthenticatedTaskStore<TContext>>());

            // WithTasks captures the store instance and never resolves it from DI, so also
            // stamp the provider when McpServerOptions is composed (first request).
            builder.Services.AddSingleton<IConfigureOptions<McpServerOptions>>(sp =>
            {
                services.Value = sp;
                return new ConfigureOptions<McpServerOptions>(NoopMcpServerOptions);
            });

            return configureTasks is null
                ? builder.WithTasks(store)
                : builder.WithTasks(store, configureTasks);
        }

        #endregion

    }

}
