using System;
using CloudNimble.McpEssentials.Redis.Tasks;
using StackExchange.Redis;

namespace CloudNimble.McpEssentials.Tests.Redis.Tasks
{

    /// <summary>
    /// Redis store whose clock is a settable <see cref="Now"/> for TTL tests.
    /// </summary>
    internal class ClockRedisStore : RedisMcpTaskStore
    {

        #region Properties

        /// <summary>
        /// Gets or sets the instant <see cref="GetUtcNow"/> returns.
        /// </summary>
        public DateTimeOffset Now { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ClockRedisStore"/> class.
        /// </summary>
        /// <param name="multiplexer">The Redis connection.</param>
        /// <param name="userId">The current user identifier.</param>
        /// <param name="now">The starting clock value.</param>
        public ClockRedisStore(IConnectionMultiplexer multiplexer, string userId, DateTimeOffset now)
            : base(multiplexer, userId)
        {
            Now = now;
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override DateTimeOffset GetUtcNow() => Now;

        #endregion

    }

}
