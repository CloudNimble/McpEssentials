using System;
using System.Security.Claims;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Adds Entity Framework Core as a task-store provider on <see cref="McpTaskStoreBuilder"/>.
    /// </summary>
    public static class McpEssentials_EntityFrameworkCore_McpTaskStoreBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Uses <see cref="EFCoreMcpTaskStore{TContext}"/> with a constant owner.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// The builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public static McpTaskStoreBuilder UseEntityFramework<TContext>(
            this McpTaskStoreBuilder builder,
            string userId,
            Action<EFCoreMcpTaskStoreOptions>? configure = null)
            where TContext : DbContext
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return builder.UseEntityFramework<TContext>(_ => userId, configure);
        }

        /// <summary>
        /// Uses <see cref="EFCoreMcpTaskStore{TContext}"/> with an owner callback.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// The builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static McpTaskStoreBuilder UseEntityFramework<TContext>(
            this McpTaskStoreBuilder builder,
            Func<string?> getCurrentUserId,
            Action<EFCoreMcpTaskStoreOptions>? configure = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return builder.UseEntityFramework<TContext>(_ => getCurrentUserId(), configure);
        }

        /// <summary>
        /// Uses <see cref="EFCoreMcpTaskStore{TContext}"/>, resolving the owner from a principal.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// The builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static McpTaskStoreBuilder UseEntityFramework<TContext>(
            this McpTaskStoreBuilder builder,
            Func<ClaimsPrincipal?> getPrincipal,
            Action<EFCoreMcpTaskStoreOptions>? configure = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.UseEntityFramework<TContext>(
                _ => McpTaskStoreBase.GetUserId(getPrincipal()),
                configure);
        }

        /// <summary>
        /// Uses <see cref="EFCoreMcpTaskStore{TContext}"/>, resolving the owner from the built provider.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="getPrincipal">
        /// Returns the principal for the current request. ASP.NET Core hosts pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// The builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public static McpTaskStoreBuilder UseEntityFramework<TContext>(
            this McpTaskStoreBuilder builder,
            Func<IServiceProvider, ClaimsPrincipal?> getPrincipal,
            Action<EFCoreMcpTaskStoreOptions>? configure = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return builder.UseEntityFramework<TContext>(
                sp => McpTaskStoreBase.GetUserId(getPrincipal(sp)),
                configure);
        }

        /// <summary>
        /// Uses <see cref="EFCoreMcpTaskStore{TContext}"/> with an owner id from the built provider.
        /// </summary>
        /// <typeparam name="TContext">The host <see cref="DbContext"/> that maps <see cref="McpTaskEntity"/>.</typeparam>
        /// <param name="builder">The task-store builder.</param>
        /// <param name="getCurrentUserId">Returns the current user identifier from the built provider.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// The builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public static McpTaskStoreBuilder UseEntityFramework<TContext>(
            this McpTaskStoreBuilder builder,
            Func<IServiceProvider, string?> getCurrentUserId,
            Action<EFCoreMcpTaskStoreOptions>? configure = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(getCurrentUserId);

            var store = new EFCoreMcpTaskStore<TContext>(
                () => builder.CapturedServices!.GetRequiredService<IServiceScopeFactory>(),
                () => getCurrentUserId(builder.CapturedServices!),
                () => builder.CapturedServices!.GetService<IMcpTaskInputSignalBus>());
            if (configure is not null)
            {
                var options = new EFCoreMcpTaskStoreOptions();
                configure(options);
                options.ApplyTo(store);
            }

            return builder.UseStore(store, captureProvider: true);
        }

        #endregion

    }

}
