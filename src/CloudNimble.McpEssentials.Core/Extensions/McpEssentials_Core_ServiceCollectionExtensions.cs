using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Extensions.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Registers Core McpEssentials services that do not depend on a specific host.
    /// </summary>
    public static class McpEssentials_Core_ServiceCollectionExtensions
    {

        #region Public Methods

        /// <summary>
        /// Registers <see cref="InMemoryMcpTaskStorePlus"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/> for a single user.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IServiceCollection AddInMemoryMcpTaskStorePlus(
            this IServiceCollection services,
            string userId,
            Action<McpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            var store = new InMemoryMcpTaskStorePlus(userId);
            McpTaskStoreOptions.Apply(configure, store);
            return RegisterStore(services, store);
        }

        /// <summary>
        /// Registers <see cref="InMemoryMcpTaskStorePlus"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddInMemoryMcpTaskStorePlus(
            this IServiceCollection services,
            Func<string?> getCurrentUserId,
            Action<McpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getCurrentUserId);

            var store = new InMemoryMcpTaskStorePlus(getCurrentUserId);
            McpTaskStoreOptions.Apply(configure, store);
            return RegisterStore(services, store);
        }

        /// <summary>
        /// Registers <see cref="InMemoryMcpTaskStorePlus"/> using a <see cref="ClaimsPrincipal"/> factory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Hosts typically pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddInMemoryMcpTaskStorePlus(
            this IServiceCollection services,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<McpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getPrincipal);

            services.TryAddSingleton(sp =>
            {
                var store = new InMemoryMcpTaskStorePlus(() => getPrincipal(sp));
                McpTaskStoreOptions.Apply(configure, store);
                return store;
            });
            services.TryAddSingleton<IMcpTaskStorePlus>(sp => sp.GetRequiredService<InMemoryMcpTaskStorePlus>());
            services.TryAddSingleton<IMcpTaskStore>(sp => sp.GetRequiredService<InMemoryMcpTaskStorePlus>());

            return services;
        }

        #endregion

        #region Private Methods

        private static IServiceCollection RegisterStore(IServiceCollection services, InMemoryMcpTaskStorePlus store)
        {
            services.TryAddSingleton(store);
            services.TryAddSingleton<IMcpTaskStorePlus>(store);
            services.TryAddSingleton<IMcpTaskStore>(store);

            return services;
        }

        #endregion

    }

}
