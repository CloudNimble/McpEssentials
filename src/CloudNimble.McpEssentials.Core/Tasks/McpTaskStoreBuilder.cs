using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Server;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// Selects an <see cref="IMcpTaskStorePlus"/> provider for <c>WithTasksPlus</c> and OData
    /// <c>WithTasks</c>. Core ships <see cref="UseInMemory(string, Action{McpTaskStoreOptions}?)"/>.
    /// Provider packages add <c>UseEntityFramework</c>, <c>UseRedis</c>, <c>UsePostgreSql</c>, and so on.
    /// </summary>
    public sealed class McpTaskStoreBuilder
    {

        #region Fields

        internal readonly StrongBox<IServiceProvider> ServicesBox = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Gets the built <see cref="IServiceProvider"/> after MCP options compose, or
        /// <see langword="null"/> until the first request.
        /// </summary>
        public IServiceProvider? CapturedServices => ServicesBox.Value;

        /// <summary>
        /// Gets the store selected by the last <c>Use*</c> call.
        /// </summary>
        public IMcpTaskStorePlus? Store { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTaskStoreBuilder"/> class.
        /// </summary>
        /// <param name="services">The application service collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        public McpTaskStoreBuilder(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            Services = services;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Uses <see cref="InMemoryMcpTaskStorePlus"/> with a constant owner.
        /// </summary>
        /// <param name="userId">The owner stamped on every task this process creates.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// This builder.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/> or whitespace.</exception>
        public McpTaskStoreBuilder UseInMemory(string userId, Action<McpTaskStoreOptions>? configure = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return UseInMemoryStore(new InMemoryMcpTaskStorePlus(userId), configure, captureProvider: false);
        }

        /// <summary>
        /// Uses <see cref="InMemoryMcpTaskStorePlus"/> with an owner callback.
        /// </summary>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// This builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public McpTaskStoreBuilder UseInMemory(Func<string?> getCurrentUserId, Action<McpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return UseInMemoryStore(new InMemoryMcpTaskStorePlus(getCurrentUserId), configure, captureProvider: false);
        }

        /// <summary>
        /// Uses <see cref="InMemoryMcpTaskStorePlus"/>, resolving the owner from a principal.
        /// </summary>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// This builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public McpTaskStoreBuilder UseInMemory(Func<ClaimsPrincipal?> getPrincipal, Action<McpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return UseInMemoryStore(new InMemoryMcpTaskStorePlus(getPrincipal), configure, captureProvider: false);
        }

        /// <summary>
        /// Uses <see cref="InMemoryMcpTaskStorePlus"/>, resolving the owner from the built
        /// <see cref="IServiceProvider"/>. ASP.NET Core hosts pass
        /// <c>sp =&gt; sp.GetRequiredService&lt;IHttpContextAccessor&gt;().HttpContext?.User</c>.
        /// </summary>
        /// <param name="getPrincipal">Returns the principal for the current request.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// This builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getPrincipal"/> is <see langword="null"/>.</exception>
        public McpTaskStoreBuilder UseInMemory(Func<IServiceProvider, ClaimsPrincipal?> getPrincipal, Action<McpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(getPrincipal);
            return UseInMemoryStore(
                new InMemoryMcpTaskStorePlus(() => getPrincipal(ServicesBox.Value!)),
                configure,
                captureProvider: true);
        }

        /// <summary>
        /// Uses <see cref="InMemoryMcpTaskStorePlus"/> with an owner id resolved from the built
        /// <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="getCurrentUserId">Returns the current user identifier, or <see langword="null"/> when unauthenticated.</param>
        /// <param name="configure">Optional store options (TTL, poll interval).</param>
        /// <returns>
        /// This builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getCurrentUserId"/> is <see langword="null"/>.</exception>
        public McpTaskStoreBuilder UseInMemory(Func<IServiceProvider, string?> getCurrentUserId, Action<McpTaskStoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(getCurrentUserId);
            return UseInMemoryStore(
                new InMemoryMcpTaskStorePlus(() => getCurrentUserId(ServicesBox.Value!)),
                configure,
                captureProvider: true);
        }

        /// <summary>
        /// Registers <paramref name="store"/> as the selected provider. Durable packages call this
        /// from their <c>Use*</c> extensions.
        /// </summary>
        /// <param name="store">The plus store.</param>
        /// <param name="captureProvider">
        /// When <see langword="true"/>, captures <see cref="IServiceProvider"/> on first options
        /// compose so owner factories can resolve scoped services.
        /// </param>
        /// <returns>
        /// This builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is <see langword="null"/>.</exception>
        public McpTaskStoreBuilder UseStore(IMcpTaskStorePlus store, bool captureProvider = true)
        {
            ArgumentNullException.ThrowIfNull(store);

            Store = store;
            object Resolve(IServiceProvider sp)
            {
                if (captureProvider)
                {
                    ServicesBox.Value = sp;
                }

                return store;
            }

            Services.Add(new ServiceDescriptor(store.GetType(), Resolve, ServiceLifetime.Singleton));
            Services.TryAddSingleton(sp => (IMcpTaskStorePlus)Resolve(sp));
            Services.TryAddSingleton(sp => (IMcpTaskStore)Resolve(sp));
            if (captureProvider)
            {
                Services.AddSingleton<IConfigureOptions<McpServerOptions>>(sp =>
                {
                    ServicesBox.Value = sp;
                    return new ConfigureOptions<McpServerOptions>(static _ => { });
                });
            }

            return this;
        }

        #endregion

        #region Private Methods

        private McpTaskStoreBuilder UseInMemoryStore(InMemoryMcpTaskStorePlus store, Action<McpTaskStoreOptions>? configure, bool captureProvider)
        {
            McpTaskStoreOptions.Apply(configure, store);
            return UseStore(store, captureProvider);
        }

        #endregion

    }

}
