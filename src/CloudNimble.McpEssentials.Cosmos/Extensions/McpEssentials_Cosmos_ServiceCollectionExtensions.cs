using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Extensions.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Registers the Cosmos authenticated MCP task store.
    /// </summary>
    public static class McpEssentials_Cosmos_ServiceCollectionExtensions
    {

        #region Public Methods

        /// <summary>
        /// Registers <see cref="CosmosMcpTaskStore"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/> for a single user.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IServiceCollection AddMcpEssentialsCosmosTasks(
            this IServiceCollection services,
            string userId,
            Action<CosmosMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            return RegisterStore(services, sp =>
            {
                var store = new CosmosMcpTaskStore(
                    () => sp.GetRequiredService<CosmosClient>(),
                    () => userId,
                    () => sp.GetService<IMcpTaskInputSignalBus>(),
                    () => sp.GetService<ICosmosTaskGateway>());
                Apply(configure, store);
                return store;
            });
        }

        /// <summary>
        /// Registers <see cref="CosmosMcpTaskStore"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddMcpEssentialsCosmosTasks(
            this IServiceCollection services,
            Func<string?> getCurrentUserId,
            Action<CosmosMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getCurrentUserId);

            return RegisterStore(services, sp =>
            {
                var store = new CosmosMcpTaskStore(
                    () => sp.GetRequiredService<CosmosClient>(),
                    getCurrentUserId,
                    () => sp.GetService<IMcpTaskInputSignalBus>(),
                    () => sp.GetService<ICosmosTaskGateway>());
                Apply(configure, store);
                return store;
            });
        }

        /// <summary>
        /// Registers <see cref="CosmosMcpTaskStore"/> using a
        /// <see cref="ClaimsPrincipal"/> factory resolved from the built provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Hosts typically pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddMcpEssentialsCosmosTasks(
            this IServiceCollection services,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<CosmosMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getPrincipal);

            return RegisterStore(services, sp =>
            {
                var store = new CosmosMcpTaskStore(
                    () => sp.GetRequiredService<CosmosClient>(),
                    () => McpTaskStoreBase.GetUserId(getPrincipal(sp)),
                    () => sp.GetService<IMcpTaskInputSignalBus>(),
                    () => sp.GetService<ICosmosTaskGateway>());
                Apply(configure, store);
                return store;
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applies <paramref name="configure"/> to a new options instance and onto <paramref name="store"/>.
        /// </summary>
        /// <param name="configure">Optional configuration.</param>
        /// <param name="store">The store.</param>
        private static void Apply(Action<CosmosMcpTaskStoreOptions>? configure, CosmosMcpTaskStore store)
        {
            if (configure is null)
            {
                return;
            }

            var options = new CosmosMcpTaskStoreOptions();
            configure(options);
            options.ApplyTo(store);
        }

        /// <summary>
        /// Registers the store as the singleton <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">Creates the store from the built provider.</param>
        /// <returns>The service collection.</returns>
        private static IServiceCollection RegisterStore(
            IServiceCollection services,
            Func<IServiceProvider, CosmosMcpTaskStore> factory)
        {
            services.TryAddSingleton(factory);
            services.TryAddSingleton<IMcpTaskStorePlus>(sp => sp.GetRequiredService<CosmosMcpTaskStore>());
            services.TryAddSingleton<IMcpTaskStore>(sp => sp.GetRequiredService<CosmosMcpTaskStore>());
            return services;
        }

        #endregion

    }

}
