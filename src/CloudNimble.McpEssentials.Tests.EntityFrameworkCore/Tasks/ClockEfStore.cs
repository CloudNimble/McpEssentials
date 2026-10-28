using System;
using CloudNimble.McpEssentials.EntityFrameworkCore.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// EF store whose clock is a settable <see cref="Now"/> for TTL and sweep tests.
    /// </summary>
    internal class ClockEfStore : EFCoreMcpTaskStore<McpTaskDbContext>
    {

        #region Properties

        /// <summary>
        /// Gets or sets the instant <see cref="GetUtcNow"/> returns.
        /// </summary>
        public DateTimeOffset Now { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ClockEfStore"/> class.
        /// </summary>
        /// <param name="scopes">Opens a scope per operation to resolve <see cref="McpTaskDbContext"/>.</param>
        /// <param name="userId">The current user identifier.</param>
        /// <param name="now">The starting clock value.</param>
        public ClockEfStore(IServiceScopeFactory scopes, string userId, DateTimeOffset now)
            : base(scopes, userId)
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
