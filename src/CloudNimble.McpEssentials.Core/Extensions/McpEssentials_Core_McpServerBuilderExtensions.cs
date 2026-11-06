using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using ModelContextProtocol.Extensions.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Enables MCP Tasks on an <see cref="IMcpServerBuilder"/> with a plus store.
    /// </summary>
    public static class McpEssentials_Core_McpServerBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Enables MCP Tasks and selects a plus store provider.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="configure">Selects <c>UseInMemory</c> or a provider <c>Use*</c> method.</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="configure"/> did not call a <c>Use*</c> method.</exception>
        /// <example>
        /// <code>
        /// builder.Services.AddMcpServer()
        ///     .WithHttpTransport()
        ///     .WithTasksPlus(tasks =&gt; tasks.UseInMemory("local-user"));
        /// </code>
        /// </example>
        public static IMcpServerBuilder WithTasksPlus(
            this IMcpServerBuilder builder,
            Action<McpTaskStoreBuilder> configure,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            var tasks = new McpTaskStoreBuilder(builder.Services);
            configure(tasks);
            if (tasks.Store is null)
            {
                throw new InvalidOperationException($"Call {nameof(McpTaskStoreBuilder.UseInMemory)} or a provider Use* method.");
            }

            return builder.WithTasks(tasks.Store, options =>
            {
                configureTasks?.Invoke(options);
                FormElicitationExecutionMode.Apply(options);
            });
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="InMemoryMcpTaskStorePlus"/> for a single user.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IMcpServerBuilder WithTasksPlus(
            this IMcpServerBuilder builder,
            string userId,
            Action<McpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return builder.WithTasksPlus(tasks => tasks.UseInMemory(userId, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="InMemoryMcpTaskStorePlus"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithTasksPlus(
            this IMcpServerBuilder builder,
            Func<string?> getCurrentUserId,
            Action<McpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return builder.WithTasksPlus(tasks => tasks.UseInMemory(getCurrentUserId, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="InMemoryMcpTaskStorePlus"/>, resolving the user from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Invoked on each client-facing task operation.
        /// Use the <see cref="WithTasksPlus(IMcpServerBuilder, Func{IServiceProvider, ClaimsPrincipal?}, Action{McpTaskStoreOptions}?, Action{McpTasksOptions}?)"/>
        /// overload when the principal comes from DI.
        /// </param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithTasksPlus(
            this IMcpServerBuilder builder,
            Func<ClaimsPrincipal?> getPrincipal,
            Action<McpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithTasksPlus(tasks => tasks.UseInMemory(getPrincipal, configure), configureTasks);
        }

        /// <summary>
        /// Enables MCP Tasks backed by <see cref="InMemoryMcpTaskStorePlus"/>, resolving the
        /// user from services already registered in the built <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="builder">The MCP server builder.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Invoked on each client-facing task operation.
        /// ASP.NET Core hosts call <c>AddHttpContextAccessor()</c> and pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <param name="configureTasks">Optional SDK Tasks options (execution-mode selector, and so on).</param>
        /// <returns>The server builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        /// <example>
        /// <code>
        /// builder.Services.AddHttpContextAccessor();
        /// builder.Services.AddMcpServer()
        ///     .WithHttpTransport()
        ///     .WithTasksPlus(
        ///         sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User,
        ///         o =&gt; o.DefaultTimeToLive = TimeSpan.FromMinutes(5));
        /// </code>
        /// </example>
        public static IMcpServerBuilder WithTasksPlus(
            this IMcpServerBuilder builder,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<McpTaskStoreOptions>? configure = null,
            Action<McpTasksOptions>? configureTasks = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.WithTasksPlus(tasks => tasks.UseInMemory(getPrincipal, configure), configureTasks);
        }

        #endregion

    }

}
