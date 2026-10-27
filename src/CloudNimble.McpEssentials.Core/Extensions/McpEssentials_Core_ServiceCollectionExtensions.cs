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
        /// Registers <see cref="InMemoryAuthenticatedMcpTaskStore"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IPerUserMcpTaskStore"/> for a single user.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IServiceCollection AddInMemoryAuthenticatedTaskStore(
            this IServiceCollection services,
            string userId,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            var store = new InMemoryAuthenticatedMcpTaskStore(userId);
            AuthenticatedMcpTaskStoreOptions.Apply(configure, store);
            return RegisterStore(services, store);
        }

        /// <summary>
        /// Registers <see cref="InMemoryAuthenticatedMcpTaskStore"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IPerUserMcpTaskStore"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddInMemoryAuthenticatedTaskStore(
            this IServiceCollection services,
            Func<string?> getCurrentUserId,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getCurrentUserId);

            var store = new InMemoryAuthenticatedMcpTaskStore(getCurrentUserId);
            AuthenticatedMcpTaskStoreOptions.Apply(configure, store);
            return RegisterStore(services, store);
        }

        /// <summary>
        /// Registers <see cref="InMemoryAuthenticatedMcpTaskStore"/> using a <see cref="ClaimsPrincipal"/> factory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Hosts typically pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddInMemoryAuthenticatedTaskStore(
            this IServiceCollection services,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getPrincipal);

            services.TryAddSingleton(sp =>
            {
                var store = new InMemoryAuthenticatedMcpTaskStore(() => getPrincipal(sp));
                AuthenticatedMcpTaskStoreOptions.Apply(configure, store);
                return store;
            });
            services.TryAddSingleton<IPerUserMcpTaskStore>(sp => sp.GetRequiredService<InMemoryAuthenticatedMcpTaskStore>());
            services.TryAddSingleton<IMcpTaskStore>(sp => sp.GetRequiredService<InMemoryAuthenticatedMcpTaskStore>());

            return services;
        }

        #endregion

        #region Private Methods

        private static IServiceCollection RegisterStore(IServiceCollection services, InMemoryAuthenticatedMcpTaskStore store)
        {
            services.TryAddSingleton(store);
            services.TryAddSingleton<IPerUserMcpTaskStore>(store);
            services.TryAddSingleton<IMcpTaskStore>(store);

            return services;
        }

        #endregion

    }

}
