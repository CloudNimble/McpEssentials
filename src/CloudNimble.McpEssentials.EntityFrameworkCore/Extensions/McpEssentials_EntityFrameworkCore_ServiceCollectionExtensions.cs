using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Extensions.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Registers the Entity Framework Core authenticated MCP task store.
    /// </summary>
    public static class McpEssentials_EntityFrameworkCore_ServiceCollectionExtensions
    {

        #region Public Methods

        /// <summary>
        /// Registers <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IPerUserMcpTaskStore"/> for a single user.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IServiceCollection AddMcpEssentialsEntityFrameworkTasks<TContext>(
            this IServiceCollection services,
            string userId,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            return RegisterStore<TContext>(services, sp =>
            {
                var store = new EntityFrameworkAuthenticatedTaskStore<TContext>(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    userId,
                    sp.GetService<IMcpTaskInputSignalBus>());
                AuthenticatedMcpTaskStoreOptions.Apply(configure, store);
                return store;
            });
        }

        /// <summary>
        /// Registers <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IPerUserMcpTaskStore"/>.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddMcpEssentialsEntityFrameworkTasks<TContext>(
            this IServiceCollection services,
            Func<string?> getCurrentUserId,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getCurrentUserId);

            return RegisterStore<TContext>(services, sp =>
            {
                var store = new EntityFrameworkAuthenticatedTaskStore<TContext>(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    getCurrentUserId,
                    sp.GetService<IMcpTaskInputSignalBus>());
                AuthenticatedMcpTaskStoreOptions.Apply(configure, store);
                return store;
            });
        }

        /// <summary>
        /// Registers <see cref="EntityFrameworkAuthenticatedTaskStore{TContext}"/> using a
        /// <see cref="ClaimsPrincipal"/> factory resolved from the built provider.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Hosts typically pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddMcpEssentialsEntityFrameworkTasks<TContext>(
            this IServiceCollection services,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<AuthenticatedMcpTaskStoreOptions>? configure = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getPrincipal);

            return RegisterStore<TContext>(services, sp =>
            {
                var store = new EntityFrameworkAuthenticatedTaskStore<TContext>(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    () => getPrincipal(sp),
                    sp.GetService<IMcpTaskInputSignalBus>());
                AuthenticatedMcpTaskStoreOptions.Apply(configure, store);
                return store;
            });
        }

        #endregion

        #region Private Methods

        private static IServiceCollection RegisterStore<TContext>(
            IServiceCollection services,
            Func<IServiceProvider, EntityFrameworkAuthenticatedTaskStore<TContext>> factory)
            where TContext : DbContext
        {
            services.TryAddSingleton(factory);
            services.TryAddSingleton<IPerUserMcpTaskStore>(sp => sp.GetRequiredService<EntityFrameworkAuthenticatedTaskStore<TContext>>());
            services.TryAddSingleton<IMcpTaskStore>(sp => sp.GetRequiredService<EntityFrameworkAuthenticatedTaskStore<TContext>>());

            return services;
        }

        #endregion

    }

}
