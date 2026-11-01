using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.PostgreSQL.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Extensions.Tasks;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Registers the PostgreSQL authenticated MCP task store.
    /// </summary>
    public static class McpEssentials_PostgreSQL_ServiceCollectionExtensions
    {

        #region Public Methods

        /// <summary>
        /// Registers <see cref="PostgreSqlMcpTaskStore"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/> for a single user.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, table, listen channel).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static IServiceCollection AddMcpEssentialsPostgreSqlTasks(
            this IServiceCollection services,
            string userId,
            Action<PostgreSqlMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            return RegisterStore(services, sp =>
            {
                var store = new PostgreSqlMcpTaskStore(sp.GetRequiredService<NpgsqlDataSource>(), userId);
                Apply(configure, store);
                return store;
            });
        }

        /// <summary>
        /// Registers <see cref="PostgreSqlMcpTaskStore"/> as the singleton
        /// <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, table, listen channel).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddMcpEssentialsPostgreSqlTasks(
            this IServiceCollection services,
            Func<string?> getCurrentUserId,
            Action<PostgreSqlMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getCurrentUserId);

            return RegisterStore(services, sp =>
            {
                var store = new PostgreSqlMcpTaskStore(sp.GetRequiredService<NpgsqlDataSource>(), getCurrentUserId);
                Apply(configure, store);
                return store;
            });
        }

        /// <summary>
        /// Registers <see cref="PostgreSqlMcpTaskStore"/> using a
        /// <see cref="ClaimsPrincipal"/> factory resolved from the built provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. Hosts typically pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, table, listen channel).</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddMcpEssentialsPostgreSqlTasks(
            this IServiceCollection services,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<PostgreSqlMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(getPrincipal);

            return RegisterStore(services, sp =>
            {
                var store = new PostgreSqlMcpTaskStore(sp.GetRequiredService<NpgsqlDataSource>(), () => getPrincipal(sp));
                Apply(configure, store);
                return store;
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applies <paramref name="configure"/> onto <paramref name="store"/>.
        /// </summary>
        private static void Apply(Action<PostgreSqlMcpTaskStoreOptions>? configure, PostgreSqlMcpTaskStore store)
        {
            if (configure is null)
            {
                return;
            }

            var options = new PostgreSqlMcpTaskStoreOptions();
            configure(options);
            options.ApplyTo(store);
        }

        /// <summary>
        /// Registers the store as the singleton <see cref="IMcpTaskStore"/> and <see cref="IMcpTaskStorePlus"/>.
        /// </summary>
        private static IServiceCollection RegisterStore(
            IServiceCollection services,
            Func<IServiceProvider, PostgreSqlMcpTaskStore> factory)
        {
            services.TryAddSingleton(factory);
            services.TryAddSingleton<IMcpTaskStorePlus>(sp => sp.GetRequiredService<PostgreSqlMcpTaskStore>());
            services.TryAddSingleton<IMcpTaskStore>(sp => sp.GetRequiredService<PostgreSqlMcpTaskStore>());
            return services;
        }

        #endregion

    }

}
