using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Adds Cosmos as a task-store provider on <see cref="McpTaskStoreBuilder"/>.
    /// </summary>
    public static class McpEssentials_Cosmos_McpTaskStoreBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Uses <see cref="CosmosMcpTaskStore"/> with a constant owner.
        /// </summary>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <returns>The builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static McpTaskStoreBuilder UseCosmos(
            this McpTaskStoreBuilder builder,
            string userId,
            Action<CosmosMcpTaskStoreOptions>? configure = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return builder.UseCosmos(_ => userId, configure);
        }

        /// <summary>
        /// Uses <see cref="CosmosMcpTaskStore"/> with an owner callback.
        /// </summary>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <returns>The builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static McpTaskStoreBuilder UseCosmos(
            this McpTaskStoreBuilder builder,
            Func<string?> getCurrentUserId,
            Action<CosmosMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return builder.UseCosmos(_ => getCurrentUserId(), configure);
        }

        /// <summary>
        /// Uses <see cref="CosmosMcpTaskStore"/>, resolving the owner from a principal.
        /// </summary>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <returns>The builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static McpTaskStoreBuilder UseCosmos(
            this McpTaskStoreBuilder builder,
            Func<ClaimsPrincipal?> getPrincipal,
            Action<CosmosMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.UseCosmos(_ => McpTaskStoreBase.GetUserId(getPrincipal()), configure);
        }

        /// <summary>
        /// Uses <see cref="CosmosMcpTaskStore"/>, resolving the owner from the built provider.
        /// </summary>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. ASP.NET Core hosts pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <returns>The builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static McpTaskStoreBuilder UseCosmos(
            this McpTaskStoreBuilder builder,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<CosmosMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.UseCosmos(sp => McpTaskStoreBase.GetUserId(getPrincipal(sp)), configure);
        }

        /// <summary>
        /// Uses <see cref="CosmosMcpTaskStore"/> with an owner id from the built provider.
        /// Resolves <see cref="CosmosClient"/> from DI.
        /// </summary>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier from the built provider.</param>
        /// <param name="configure">Optional store options (TTL, database and container ids).</param>
        /// <returns>The builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static McpTaskStoreBuilder UseCosmos(
            this McpTaskStoreBuilder builder,
            Func<IServiceProvider, string?> getCurrentUserId,
            Action<CosmosMcpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(getCurrentUserId);

            var store = new CosmosMcpTaskStore(
                () => builder.CapturedServices!.GetRequiredService<CosmosClient>(),
                () => getCurrentUserId(builder.CapturedServices!),
                () => builder.CapturedServices!.GetService<IMcpTaskInputSignalBus>(),
                () => builder.CapturedServices!.GetService<ICosmosTaskGateway>());
            if (configure is not null)
            {
                var options = new CosmosMcpTaskStoreOptions();
                configure(options);
                options.ApplyTo(store);
            }

            return builder.UseStore(store, captureProvider: true);
        }

        #endregion

    }

}
