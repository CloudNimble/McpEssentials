using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Enables authenticated MCP Tasks on an <see cref="IMcpServerBuilder"/>.
    /// </summary>
    public static class McpEssentials_Core_McpServerBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="InMemoryAuthenticatedMcpTaskStore"/> for a single user.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IMcpServerBuilder WithAuthenticatedTasks(this IMcpServerBuilder builder, string userId, Action<McpTasksOptions>? configure = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return builder.WithAuthenticatedTasks(new InMemoryAuthenticatedMcpTaskStore(userId), configure);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="InMemoryAuthenticatedMcpTaskStore"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithAuthenticatedTasks(this IMcpServerBuilder builder, Func<string?> getCurrentUserId, Action<McpTasksOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return builder.WithAuthenticatedTasks(new InMemoryAuthenticatedMcpTaskStore(getCurrentUserId), configure);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="InMemoryAuthenticatedMcpTaskStore"/>, resolving the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Invoked on each client-facing task operation.
        /// Use the <see cref="WithAuthenticatedTasks(IMcpServerBuilder, Func{IServiceProvider, ClaimsPrincipal?}, Action{McpTasksOptions}?)"/>
        /// overload when the principal comes from DI.
        /// </param>
        /// <param name="configure">Optional Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithAuthenticatedTasks(this IMcpServerBuilder builder, Func<ClaimsPrincipal?> getPrincipal, Action<McpTasksOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithAuthenticatedTasks(new InMemoryAuthenticatedMcpTaskStore(getPrincipal), configure);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="InMemoryAuthenticatedMcpTaskStore"/>, resolving the
        /// user from services already registered in the built <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Invoked on each client-facing task operation.
        /// ASP.NET Core hosts call <c>AddHttpContextAccessor()</c> and pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        /// <example>
        /// <code>
        /// builder.Services.AddHttpContextAccessor();
        /// builder.Services.AddMcpServer()
        ///     .WithHttpTransport()
        ///     .WithAuthenticatedTasks(sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User);
        /// </code>
        /// </example>
        public static IMcpServerBuilder WithAuthenticatedTasks(this IMcpServerBuilder builder, Func<IServiceProvider, ClaimsPrincipal?> getPrincipal, Action<McpTasksOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(getPrincipal);

            var services = new StrongBox<IServiceProvider>();
            var store = new InMemoryAuthenticatedMcpTaskStore(() => getPrincipal(services.Value!));

            builder.Services.TryAddSingleton(sp =>
            {
                services.Value = sp;
                return store;
            });
            builder.Services.TryAddSingleton<IPerUserMcpTaskStore>(sp => sp.GetRequiredService<InMemoryAuthenticatedMcpTaskStore>());
            builder.Services.TryAddSingleton<IMcpTaskStore>(sp => sp.GetRequiredService<InMemoryAuthenticatedMcpTaskStore>());

            // WithTasks captures the store instance and never resolves it from DI, so also
            // stamp the provider when McpServerOptions is composed (first request).
            builder.Services.AddSingleton<IConfigureOptions<McpServerOptions>>(sp =>
            {
                services.Value = sp;
                return new ConfigureOptions<McpServerOptions>(NoopMcpServerOptions);
            });

            return configure is null
                ? builder.WithTasks(store)
                : builder.WithTasks(store, configure);
        }

        #endregion

        #region Private Methods

        private static void NoopMcpServerOptions(McpServerOptions _)
        {
        }

        private static IMcpServerBuilder WithAuthenticatedTasks(this IMcpServerBuilder builder, InMemoryAuthenticatedMcpTaskStore store, Action<McpTasksOptions>? configure)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.TryAddSingleton(store);
            builder.Services.TryAddSingleton<IPerUserMcpTaskStore>(store);
            builder.Services.TryAddSingleton<IMcpTaskStore>(store);

            return configure is null
                ? builder.WithTasks(store)
                : builder.WithTasks(store, configure);
        }

        #endregion

    }

}
