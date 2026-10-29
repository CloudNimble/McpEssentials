using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Redis.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Extensions.Tasks;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Registers the Redis authenticated MCP task store.
    /// </summary>
    public static class McpEssentials_Redis_ServiceCollectionExtensions
    {

        #region Public Methods

        /// <summary>
        /// Registers <see cref="RedisMcpTaskStore"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/> for a single user.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, key prefix, replica wait).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IServiceCollection AddMcpEssentialsRedisTasks(
            this IServiceCollection services,
            string userId,
            Action<RedisMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            return RegisterStore(services, sp =>
            {
                var store = new RedisMcpTaskStore(sp.GetRequiredService<IConnectionMultiplexer>(), userId);
                Apply(configure, store);
                return store;
            });
        }

        /// <summary>
        /// Registers <see cref="RedisMcpTaskStore"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, key prefix, replica wait).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddMcpEssentialsRedisTasks(
            this IServiceCollection services,
            Func<string?> getCurrentUserId,
            Action<RedisMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getCurrentUserId);

            return RegisterStore(services, sp =>
            {
                var store = new RedisMcpTaskStore(sp.GetRequiredService<IConnectionMultiplexer>(), getCurrentUserId);
                Apply(configure, store);
                return store;
            });
        }

        /// <summary>
        /// Registers <see cref="RedisMcpTaskStore"/> using a
        /// <see cref="ClaimsPrincipal"/> factory resolved from the built provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Hosts typically pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, key prefix, replica wait).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddMcpEssentialsRedisTasks(
            this IServiceCollection services,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<RedisMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getPrincipal);

            return RegisterStore(services, sp =>
            {
                var store = new RedisMcpTaskStore(sp.GetRequiredService<IConnectionMultiplexer>(), () => getPrincipal(sp));
                Apply(configure, store);
                return store;
            });
        }

        #endregion

        #region Private Methods

        private static void Apply(Action<RedisMcpTaskStoreOptions>? configure, RedisMcpTaskStore store)
        {
            if (configure is null)
            {
                return;
            }

            var options = new RedisMcpTaskStoreOptions();
            configure(options);
            options.ApplyTo(store);
        }

        private static IServiceCollection RegisterStore(
            IServiceCollection services,
            Func<IServiceProvider, RedisMcpTaskStore> factory)
        {
            services.TryAddSingleton(factory);
            services.TryAddSingleton<IMcpTaskStorePlus>(sp => sp.GetRequiredService<RedisMcpTaskStore>());
            services.TryAddSingleton<IMcpTaskStore>(sp => sp.GetRequiredService<RedisMcpTaskStore>());
            return services;
        }

        #endregion

    }

}
