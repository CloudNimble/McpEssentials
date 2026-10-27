using System;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// Registration knobs for authenticated MCP task stores. Passed to
    /// <c>AddInMemoryAuthenticatedTaskStore</c>, <c>WithAuthenticatedTasks</c>,
    /// and the Entity Framework equivalents.
    /// </summary>
    public class AuthenticatedMcpTaskStoreOptions
    {

        #region Properties

        /// <summary>
        /// Gets or sets the default time-to-live for new tasks, or <see langword="null"/> for unlimited.
        /// </summary>
        /// <remarks>
        /// When set to a positive value, the work window ends at <c>CreatedAt + TimeToLive</c> and
        /// the row is discarded at <c>CreatedAt + 2×TimeToLive</c>, or sooner when a terminal get
        /// serves it. A <see langword="null"/> or non-positive value keeps tasks until a terminal
        /// get or until the store discards them.
        /// </remarks>
        public TimeSpan? DefaultTimeToLive { get; set; }

        /// <summary>
        /// Gets or sets the default poll interval in milliseconds for new tasks.
        /// </summary>
        /// <value>The default is 1000 milliseconds.</value>
        public long DefaultPollIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether the store pulses a throttled flush after create and on get.
        /// </summary>
        /// <value>The default is <see langword="true"/>.</value>
        /// <remarks>
        /// Set to <see langword="false"/> and call <c>SweepExpiredAsync</c> from your own job
        /// when you want to own the schedule. The public flush handle always runs a full pass.
        /// </remarks>
        public bool AutoSweep { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum interval between automatic flush pulses.
        /// </summary>
        /// <value>The default is 30 seconds. <see cref="TimeSpan.Zero"/> skips the throttle.</value>
        public TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether an owner get of a terminal snapshot serves it and then drops the row.
        /// </summary>
        /// <value>The default is <see langword="true"/>.</value>
        public bool ServeAndDrop { get; set; } = true;

        #endregion

        #region Public Methods

        /// <summary>
        /// Copies these values onto <paramref name="store"/>.
        /// </summary>
        /// <param name="store">The store to configure.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is <see langword="null"/>.</exception>
        public virtual void ApplyTo(AuthenticatedMcpTaskStoreBase store)
        {
            ArgumentNullException.ThrowIfNull(store);
            store.DefaultTimeToLive = DefaultTimeToLive;
            store.DefaultPollIntervalMs = DefaultPollIntervalMs;
            store.AutoSweep = AutoSweep;
            store.SweepInterval = SweepInterval;
            store.ServeAndDrop = ServeAndDrop;
        }

        /// <summary>
        /// Builds options from <paramref name="configure"/> and copies them onto <paramref name="store"/>.
        /// Does nothing when <paramref name="configure"/> is <see langword="null"/>.
        /// </summary>
        /// <param name="configure">Optional callback that sets TTL, poll interval, and flush knobs.</param>
        /// <param name="store">The store to configure.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is <see langword="null"/>.</exception>
        public static void Apply(Action<AuthenticatedMcpTaskStoreOptions>? configure, AuthenticatedMcpTaskStoreBase store)
        {
            ArgumentNullException.ThrowIfNull(store);
            if (configure is null)
            {
                return;
            }

            var options = new AuthenticatedMcpTaskStoreOptions();
            configure(options);
            options.ApplyTo(store);
        }

        #endregion

    }

}
